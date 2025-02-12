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
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using FastMember;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class ImportGoldModelActivity : IDisposable
{
    private static readonly string[] _columnOrder =
    [
        "Id",
        "metering_point_id",
        "valid_from_date",
        "valid_to_date",
        "dh2_created",
        "metering_grid_area_id",
        "metering_point_state_id",
        "btd_business_trans_doss_id",
        "physical_status_of_mp",
        "type_of_mp",
        "sub_type_of_mp",
        "energy_timeseries_measure_unit",
        "web_access_code",
        "balance_supplier_id",
    ];

    private readonly BlockingCollection<dynamic> _importCollection = new(500000);
    private readonly BlockingCollection<IDataReader> _submitCollection = new(5);

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

    [Function(nameof(ImportGoldModelActivity))]
    public async Task RunAsync([ActivityTrigger] ImportGoldModelActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var sw = Stopwatch.StartNew();

        await ImportAsync(input.MaxTransDossId).ConfigureAwait(false);

        _logger.LogWarning("Gold model imported in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        _importCollection.Dispose();
        _submitCollection.Dispose();
    }

    private async Task ImportAsync(long maxTransDossId)
    {
        var importSilver = Task.Run(async () =>
        {
            try
            {
                await ImportDataAsync(maxTransDossId).ConfigureAwait(false);
            }
            catch
            {
                _importCollection.Dispose();
                throw;
            }
        });

        var goldTransform = Task.Run(() =>
        {
            try
            {
                PackageRecords();
            }
            catch
            {
                _submitCollection.Dispose();
                throw;
            }
        });

        var bulkInsert = Task.Run(BulkInsertAsync);

        await Task.WhenAll(importSilver, goldTransform, bulkInsert).ConfigureAwait(false);
    }

    private async Task ImportDataAsync(long maximumBusinessTransDossId)
    {
        var query = DatabricksStatement.FromRawSql(
            $"""
             SELECT
                CAST(metering_point_id as BIGINT) AS metering_point_id,
                valid_from_date,
                valid_to_date,
                CAST(dh2_created as timestamp) as dh2_created,
                metering_grid_area_id,
                metering_point_state_id,
                btd_business_trans_doss_id,
                physical_status_of_mp,
                type_of_mp,
                sub_type_of_mp,
                energy_timeseries_measure_unit,
                web_access_code,
                balance_supplier_id

             FROM migrations_electricity_market.electricity_market_metering_points_view_v2
             WHERE btd_business_trans_doss_id < {maximumBusinessTransDossId}
             """);

        var sw = Stopwatch.StartNew();
        var firstLog = true;

        var results = _databricksSqlWarehouseQueryExecutor
            .ExecuteStatementAsync(query.Build())
            .ConfigureAwait(false);

        await foreach (var record in results)
        {
            if (firstLog)
            {
                _logger.LogWarning("Databricks first result after {FirstResult} ms.", sw.ElapsedMilliseconds);
                firstLog = false;
            }

            _importCollection.Add(record);
        }

        _logger.LogWarning("All databricks results added after {DatabricksCompleted} ms.", sw.ElapsedMilliseconds);
        _importCollection.CompleteAdding();
    }

    private void PackageRecords()
    {
        const int capacity = 50000;

        var sw = Stopwatch.StartNew();
        var batch = new List<ImportedTransactionEntity>(capacity);

        foreach (var record in _importCollection.GetConsumingEnumerable())
        {
            if (batch.Count == capacity)
            {
                _submitCollection.Add(ObjectReader.Create(batch, _columnOrder));
                _logger.LogWarning("A batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

                sw = Stopwatch.StartNew();
                batch = new List<ImportedTransactionEntity>(capacity);
            }

            batch.Add(new ImportedTransactionEntity
            {
                metering_point_id = record.metering_point_id,
                valid_from_date = record.valid_from_date,
                valid_to_date = record.valid_to_date ?? DateTimeOffset.MaxValue,
                dh2_created = record.dh2_created,
                metering_grid_area_id = record.metering_grid_area_id,
                metering_point_state_id = record.metering_point_state_id,
                btd_business_trans_doss_id = record.btd_business_trans_doss_id,
                physical_status_of_mp = record.physical_status_of_mp,
                type_of_mp = record.type_of_mp,
                sub_type_of_mp = record.sub_type_of_mp,
                energy_timeseries_measure_unit = record.energy_timeseries_measure_unit,
                web_access_code = record.web_access_code,
                balance_supplier_id = record.balance_supplier_id,
            });
        }

        _submitCollection.Add(ObjectReader.Create(batch, _columnOrder));
        _logger.LogWarning("Final batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

        _submitCollection.CompleteAdding();
    }

    private async Task BulkInsertAsync()
    {
        using var bulkCopy = new SqlBulkCopy(
            _databaseOptions.Value.ConnectionString,
            SqlBulkCopyOptions.TableLock);

        bulkCopy.DestinationTableName = "electricitymarket.GoldenImport";
        bulkCopy.BulkCopyTimeout = 0;

        foreach (var batch in _submitCollection.GetConsumingEnumerable())
        {
            var sw = Stopwatch.StartNew();

            await bulkCopy.WriteToServerAsync(batch).ConfigureAwait(false);
            batch.Dispose();

            _logger.LogWarning("A batch was inserted in {InsertTime} ms.", sw.ElapsedMilliseconds);
        }
    }
}
