// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution;
using Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.Statement;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Helpers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using FastMember;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

internal sealed class ImportGoldModelActivity : IDisposable
{
    public const string ActivityName = "ImportGoldModelActivityV6";

    private readonly BlockingCollection<ExpandoObject> _importCollection = new(500_000);
    private readonly BlockingCollection<IDataReader> _submitCollection = new(2);

    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly ILogger<ImportGoldModelActivity> _logger;

    public ImportGoldModelActivity(
        IOptions<DatabaseOptions> databaseOptions,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        ILogger<ImportGoldModelActivity> logger)
    {
        _databaseOptions = databaseOptions;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _logger = logger;
    }

    [Function(ActivityName)]
    public async Task RunAsync([ActivityTrigger] ImportGoldModelActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var sw = Stopwatch.StartNew();

        await ImportAsync(input.StatementId, input.Chunk).ConfigureAwait(false);

        _logger.LogWarning("Gold model imported in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        _importCollection.Dispose();
        _submitCollection.Dispose();
    }

    private async Task ImportAsync(string statementId, Chunks chunk)
    {
        var importSilver = Task.Run(async () =>
        {
            try
            {
                await ImportDataAsync(statementId, chunk).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReleaseBlockingCollections();
                _logger.LogError(ex, "Error during ImportDataAsync.");
                throw;
            }
        });

        var goldTransform = Task.Run(() =>
        {
            try
            {
                PackageRecords(chunk);
            }
            catch (Exception ex)
            {
                ReleaseBlockingCollections();
                _logger.LogError(ex, "Error during PackageRecords.");
                throw;
            }
        });

        var bulkInsert = Task.Run(async () =>
        {
            try
            {
                await BulkInsertAsync(chunk).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReleaseBlockingCollections();
                _logger.LogError(ex, "Error during BulkInsertAsync.");
                throw;
            }
        });

        await Task.WhenAll(importSilver, goldTransform, bulkInsert).ConfigureAwait(false);

        void ReleaseBlockingCollections()
        {
            _importCollection.CompleteAdding();
            _importCollection.Dispose();
            _submitCollection.CompleteAdding();
            _submitCollection.Dispose();
        }
    }

    private async Task ImportDataAsync(string statementId, Chunks chunk)
    {
        var sw = Stopwatch.StartNew();
        var firstLog = true;

        var retryCount = 0;
Retry:

        ConfiguredCancelableAsyncEnumerable<dynamic> results;

        try
        {
            results = _databricksSqlWarehouseQueryExecutor
                .ExecuteChunkyStatementAsync(statementId, chunk)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            retryCount++;

            if (retryCount < 2)
            {
                await Task.Delay(TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                goto Retry;
            }

            throw;
        }

        await foreach (var record in results)
        {
            if (firstLog)
            {
                _logger.LogWarning("Databricks {ActivityChunk} first result after {FirstResult} ms.", chunk.chunk_index, sw.ElapsedMilliseconds);
                firstLog = false;
            }

            _importCollection.Add(record);
        }

        _logger.LogWarning("Databricks {ActivityChunk} results added after {DatabricksCompleted} ms.", chunk.chunk_index, sw.ElapsedMilliseconds);
        _importCollection.CompleteAdding();
    }

    private void PackageRecords(Chunks chunk)
    {
        var lookup = ImportModelHelper.ImportFields.ToImmutableDictionary(k => k.Key, v => v.Value);

        const int capacity = 50000;

        var sw = Stopwatch.StartNew();
        var batch = new List<ImportedTransactionEntity>(capacity);

        var columnOrder = ImportModelHelper.ImportFields
            .Select(f => f.Key)
            .ToArray();

        foreach (var record in _importCollection.GetConsumingEnumerable())
        {
            if (batch.Count == capacity)
            {
                _submitCollection.Add(ObjectReader.Create(batch, columnOrder));
                _logger.LogWarning("A batch {ActivityChunk} was prepared in {BatchTime} ms.", chunk.chunk_index, sw.ElapsedMilliseconds);

                sw = Stopwatch.StartNew();
                batch = new List<ImportedTransactionEntity>(capacity);
            }

            var importedTransaction = new ImportedTransactionEntity();

            foreach (var keyValuePair in record)
            {
                lookup[keyValuePair.Key](keyValuePair.Value, importedTransaction);
            }

            batch.Add(importedTransaction);
        }

        _submitCollection.Add(ObjectReader.Create(batch, columnOrder));
        _logger.LogWarning("A batch {ActivityChunk} was prepared in {BatchTime} ms.", chunk.chunk_index, sw.ElapsedMilliseconds);

        _submitCollection.CompleteAdding();
    }

    private async Task BulkInsertAsync(Chunks chunk)
    {
        using var bulkCopy = new SqlBulkCopy(
            _databaseOptions.Value.ConnectionString,
            SqlBulkCopyOptions.Default);

        bulkCopy.DestinationTableName = "electricitymarket.GoldenImport";
        bulkCopy.BulkCopyTimeout = 0;

        foreach (var batch in _submitCollection.GetConsumingEnumerable())
        {
            var sw = Stopwatch.StartNew();

            await bulkCopy.WriteToServerAsync(batch).ConfigureAwait(false);
            batch.Dispose();

            _logger.LogWarning("A batch {ActivityChunk} was inserted in {InsertTime} ms.", chunk.chunk_index, sw.ElapsedMilliseconds);
        }
    }
}
