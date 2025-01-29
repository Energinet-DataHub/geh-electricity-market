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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using FastMember;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class GoldenImportHandler : IGoldenImportHandler, IDisposable
{
    private readonly BlockingCollection<dynamic> _importCollection = new(1000000);
    private readonly BlockingCollection<IDataReader> _submitCollection = new(3);

    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly ILogger<GoldenImportHandler> _logger;

    public GoldenImportHandler(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        ILogger<GoldenImportHandler> logger)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _logger = logger;
    }

    public async Task ImportAsync()
    {
        var importSilver = Task.Run(() => ImportDataAsync(339762452));
        var goldTransform = Task.Run(PackageRecords);
        var bulkInsert = Task.Run(BulkInsertAsync);

        await Task.WhenAll(importSilver, goldTransform, bulkInsert).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _importCollection.Dispose();
        _submitCollection.Dispose();
    }

    private static async Task ConfigureTraceAsync(SqlConnection connection)
    {
        var toggleFlagCommand = new SqlCommand("DBCC TRACEON(610)", connection);

        await using (toggleFlagCommand.ConfigureAwait(false))
        {
            await toggleFlagCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
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
                energy_timeseries_measure_unit

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
        const int capacity = 750000;

        var sw = Stopwatch.StartNew();
        var batch = new List<MeteringPointGoldenTransaction>(capacity);

        foreach (var record in _importCollection)
        {
            if (batch.Count == capacity)
            {
                _submitCollection.Add(ObjectReader.Create(batch));
                _logger.LogWarning("A batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

                sw = Stopwatch.StartNew();
                batch = new List<MeteringPointGoldenTransaction>(capacity);
            }

            batch.Add(new MeteringPointGoldenTransaction
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
                energy_timeseries_measure_unit = record.energy_timeseries_measure_unit
            });
        }

        _submitCollection.Add(ObjectReader.Create(batch));
        _logger.LogWarning("Final batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

        _submitCollection.CompleteAdding();
    }

    private async Task BulkInsertAsync()
    {
        var connection = new SqlConnection(_electricityMarketDatabaseContext.Database.GetConnectionString());

        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await ConfigureTraceAsync(connection).ConfigureAwait(false);

            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null);

            bulkCopy.DestinationTableName = "electricitymarket.GoldenImport";
            bulkCopy.BulkCopyTimeout = 0;

            foreach (var batch in _submitCollection)
            {
                var sw = Stopwatch.StartNew();

                await bulkCopy.WriteToServerAsync(batch).ConfigureAwait(false);
                batch.Dispose();

                _logger.LogWarning("A batch was inserted in {InsertTime} ms.", sw.ElapsedMilliseconds);
            }
        }
    }

#pragma warning disable SA1300, CA1707
    private readonly struct MeteringPointGoldenTransaction
    {
        public int Id { get; init; }
        public long metering_point_id { get; init; }
        public DateTimeOffset valid_from_date { get; init; }
        public DateTimeOffset valid_to_date { get; init; }
        public DateTimeOffset dh2_created { get; init; }
        public string metering_grid_area_id { get; init; }
        public long metering_point_state_id { get; init; }
        public long btd_business_trans_doss_id { get; init; }
        public string physical_status_of_mp { get; init; }
        public string type_of_mp { get; init; }
        public string sub_type_of_mp { get; init; }
        public string energy_timeseries_measure_unit { get; init; }
    }
#pragma warning restore SA1300, CA1707
}
