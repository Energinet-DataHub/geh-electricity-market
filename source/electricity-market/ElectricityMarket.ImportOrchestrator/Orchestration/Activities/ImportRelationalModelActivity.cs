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
using System.Data;
using System.Diagnostics;
using System.Globalization;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using FastMember;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class ImportRelationalModelActivity : IDisposable
{
    private readonly BlockingCollection<dynamic> _importCollection = new(5000000);
    private readonly BlockingCollection<IDataReader> _meteringPointSubmitCollection = new(10);

    private readonly ILogger<ImportRelationalModelActivity> _logger;
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly IDbContextFactory<ElectricityMarketDatabaseContext> _contextFactory;
    private readonly IEnumerable<ITransactionImporter> _transactionImporters;

    public ImportRelationalModelActivity(
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<ImportRelationalModelActivity> logger,
        IDbContextFactory<ElectricityMarketDatabaseContext> contextFactory,
        IEnumerable<ITransactionImporter> transactionImporters)
    {
        _databaseOptions = databaseOptions;
        _contextFactory = contextFactory;
        _logger = logger;
        _transactionImporters = transactionImporters;
    }

    [Function(nameof(ImportRelationalModelActivity))]
    public async Task RunAsync([ActivityTrigger] ImportRelationalModelActivityInput input)
    {
        var read = Task.Run(async () =>
        {
            try
            {
                await ReadBulkAsync(input).ConfigureAwait(false);
            }
            catch
            {
                _importCollection.Dispose();
                throw;
            }
        });

        var package = Task.Run(async () =>
        {
            try
            {
                await PackageBulkAsync().ConfigureAwait(false);
                // await Task.CompletedTask.ConfigureAwait(false);
            }
            catch
            {
                _meteringPointSubmitCollection.Dispose();
                throw;
            }
        });

        var write = Task.Run(async () =>
        {
            await WriteBulkMeteringPointsAsync().ConfigureAwait(false);
            // write mpps, comms, esps, etc.
        });

        await Task.WhenAll(read, package, write).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _importCollection.Dispose();
        _meteringPointSubmitCollection.Dispose();
    }

    private static MeteringPointTransaction CreateMeteringPointTransaction(ImportedTransactionEntity record)
    {
        return new MeteringPointTransaction(
            record.metering_point_id.ToString(CultureInfo.InvariantCulture),
            record.valid_from_date,
            record.valid_to_date,
            record.dh2_created,
            record.metering_grid_area_id.TrimEnd(),
            record.metering_point_state_id,
            record.btd_business_trans_doss_id,
            record.physical_status_of_mp.TrimEnd(),
            record.type_of_mp.TrimEnd(),
            record.sub_type_of_mp.TrimEnd(),
            record.energy_timeseries_measure_unit.TrimEnd(),
            record.web_access_code?.TrimEnd(),
            record.balance_supplier_id?.TrimEnd());
    }

    private async Task ReadBulkAsync(ImportRelationalModelActivityInput input)
    {
        var readContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        await using (readContext)
        {
            var meteringPointsQuery = readContext.ImportedTransactions
                .AsNoTracking()
                .Select(t => t.metering_point_id)
                .Distinct()
                .OrderBy(x => x)
                .Skip(input.Skip)
                .Take(input.Take);

            var query = readContext.ImportedTransactions
                .AsNoTracking()
                .Join(
                    meteringPointsQuery,
                    outer => outer.metering_point_id,
                    inner => inner,
                    (entity, _) => entity)
                .OrderBy(t => t.metering_point_id)
                .ThenBy(t => t.btd_business_trans_doss_id)
                .ThenBy(t => t.metering_point_state_id)
                .AsAsyncEnumerable();

            var currentMeteringPointId = string.Empty;
            var list = new List<MeteringPointTransaction>();

            var c = 0L;
            await foreach (var record in query)
            {
                var meteringPointTransaction = CreateMeteringPointTransaction(record);

                if (list.Count != 0 && currentMeteringPointId != meteringPointTransaction.Identification)
                {
                    _importCollection.Add(await BulkSaveMeteringPointAsync(input.Skip + ++c, currentMeteringPointId, list).ConfigureAwait(false));
                    list.Clear();
                }

                currentMeteringPointId = meteringPointTransaction.Identification;
                list.Add(meteringPointTransaction);
            }

            _importCollection.Add(await BulkSaveMeteringPointAsync(input.Skip + ++c, currentMeteringPointId, list).ConfigureAwait(false));
            _importCollection.CompleteAdding();
        }
    }

    private Task PackageBulkAsync()
    {
        const int capacity = 50000;

        var sw = Stopwatch.StartNew();
        var batch = new List<MeteringPointEntity>(capacity);

        foreach (var record in _importCollection.GetConsumingEnumerable())
        {
            if (batch.Count == capacity)
            {
                _meteringPointSubmitCollection.Add(ObjectReader.Create(batch, "Id", "Identification"));
                _logger.LogWarning("A batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

                sw = Stopwatch.StartNew();
                batch = new List<MeteringPointEntity>(capacity);
            }

            batch.Add(new MeteringPointEntity
            {
                Id = record.Id,
                Identification = record.Identification,
            });
        }

        _meteringPointSubmitCollection.Add(ObjectReader.Create(batch, "Id", "Identification"));
        _logger.LogWarning("Final batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

        _meteringPointSubmitCollection.CompleteAdding();

        return Task.CompletedTask;
    }

    private async Task WriteBulkMeteringPointsAsync()
    {
        foreach (var batch in _meteringPointSubmitCollection.GetConsumingEnumerable())
        {
            using (batch)
            {
                var connection = new SqlConnection(_databaseOptions.Value.ConnectionString);

                await using (connection.ConfigureAwait(false))
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

                    await using (transaction.ConfigureAwait(false))
                    {
                        using var bulkCopy = new SqlBulkCopy(
                            connection,
                            SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.KeepIdentity,
                            (SqlTransaction)transaction);

                        bulkCopy.DestinationTableName = "electricitymarket.MeteringPoint";
                        bulkCopy.BulkCopyTimeout = 0;

                        var sw = Stopwatch.StartNew();

                        await bulkCopy.WriteToServerAsync(batch).ConfigureAwait(false);

                        _logger.LogWarning("A MP batch was inserted in {InsertTime} ms.", sw.ElapsedMilliseconds);

                        await transaction.CommitAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }

    private async Task<MeteringPointEntity> BulkSaveMeteringPointAsync(long id, string currentMeteringPointId, List<MeteringPointTransaction> list)
    {
        var mp = new MeteringPointEntity
        {
            Id = id,
            Identification = currentMeteringPointId,
        };

        foreach (var mpt in list)
        {
            await RunImportChainAsync(mp, mpt).ConfigureAwait(false);
        }

        return mp;

        async Task RunImportChainAsync(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
        {
            var handled = false;

            foreach (var transactionImporter in _transactionImporters)
            {
                var result = await transactionImporter.ImportAsync(meteringPoint, meteringPointTransaction).ConfigureAwait(false);

                handled |= result.Status == TransactionImporterResultStatus.Handled;
            }

            if (!handled)
            {
                _logger.LogWarning("Unhandled transaction trans_dos_id: {TransactionId} state_id: {StateId}", meteringPointTransaction.BusinessTransactionDosId, meteringPointTransaction.MeteringPointStateId);
            }
        }
    }
}
