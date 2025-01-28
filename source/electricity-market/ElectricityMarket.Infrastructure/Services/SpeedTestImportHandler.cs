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
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class SpeedTestImportHandler : ISpeedTestImportHandler
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly ILogger<SpeedTestImportHandler> _speedtestLogger;

    public SpeedTestImportHandler(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        ILogger<SpeedTestImportHandler> speedtestLogger)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _speedtestLogger = speedtestLogger;
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        var importState = await _electricityMarketDatabaseContext
            .SpeedTestImportEntities
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!importState.Enabled)
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        var offset = importState.Offset;
        var timeToFirstResult = true;

        var results = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(
            DatabricksStatement.FromRawSql(
                $"""
                 SELECT metering_point_id, valid_from_date, valid_to_date, dh3_created, metering_grid_area_id, metering_point_state_id, btd_business_trans_doss_id
                 FROM migrations_electricity_market.electricity_market_metering_points_view_v2
                 WHERE btd_business_trans_doss_id <= 339762452
                 """).Build(),
            cancellationToken);

        var connString = _electricityMarketDatabaseContext.Database.GetConnectionString();

        using var bulkCopy = new SqlBulkCopy(connString, SqlBulkCopyOptions.TableLock);
        bulkCopy.DestinationTableName = "electricitymarket.SpeedTestGold";
        bulkCopy.BulkCopyTimeout = 0;

        var batch = new DataTable();
        ConfigureColumns(batch);
        var previousJob = Task.CompletedTask;

        await foreach (var record in results.ConfigureAwait(false))
        {
            if (timeToFirstResult)
            {
                _speedtestLogger.LogWarning("Time to first result: {ElapsedMs} ms.", sw.ElapsedMilliseconds);
                timeToFirstResult = false;
            }

            if (batch.Rows.Count == 2000000)
            {
                _speedtestLogger.LogWarning("Batch ready at: {ElapsedMs} ms.", sw.ElapsedMilliseconds);

                await previousJob.ConfigureAwait(false);
                _speedtestLogger.LogWarning("Done waiting for previous job: {ElapsedMs} ms.", sw.ElapsedMilliseconds);

                var capture = batch;
                previousJob = Task.Run(() => bulkCopy.WriteToServerAsync(capture, cancellationToken).ContinueWith(_ => capture.Dispose(), TaskScheduler.Default), cancellationToken);
                _speedtestLogger.LogWarning("Done scheduling new job: {ElapsedMs} ms.", sw.ElapsedMilliseconds);

                batch = new DataTable();
                ConfigureColumns(batch);
            }

            var meteringPointId = (string)record.metering_point_id;
            var validFrom = (DateTimeOffset)record.valid_from_date;
            var validTo = record.valid_to_date == null ? DateTimeOffset.MaxValue : (DateTimeOffset)record.valid_to_date;
            var createdDate = (DateTimeOffset)record.dh3_created;
            var gridArea = (string)record.metering_grid_area_id;
            var stateId = (long)record.metering_point_state_id;
            var transDossId = (long)record.btd_business_trans_doss_id;

            var row = batch.NewRow();
            row[0] = 0;
            row[1] = meteringPointId;
            row[2] = validFrom;
            row[3] = validTo;
            row[4] = createdDate;
            row[5] = gridArea;
            row[6] = stateId;
            row[7] = transDossId;

            batch.Rows.Add(row);
            offset++;
        }

        if (batch.Rows.Count != 0)
        {
            _speedtestLogger.LogWarning("Job before final batch: {ElapsedMs} ms.", sw.ElapsedMilliseconds);
            await previousJob.ConfigureAwait(false);

            _speedtestLogger.LogWarning("Final batch beings: {ElapsedMs} ms.", sw.ElapsedMilliseconds);
            await bulkCopy.WriteToServerAsync(batch, cancellationToken).ConfigureAwait(false);
            _speedtestLogger.LogWarning("Final batch done: {ElapsedMs} ms.", sw.ElapsedMilliseconds);
        }

        batch.Dispose();

        importState.Enabled = false;
        importState.Offset = offset;

        await _electricityMarketDatabaseContext
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }

    private static void ConfigureColumns(DataTable batch)
    {
        batch.Columns.Add("Id", typeof(int));
        batch.Columns.Add("MeteringPointId", typeof(string));
        batch.Columns.Add("ValidFrom", typeof(DateTimeOffset));
        batch.Columns.Add("ValidTo", typeof(DateTimeOffset));
        batch.Columns.Add("CreatedDate", typeof(DateTimeOffset));
        batch.Columns.Add("GridArea", typeof(string));
        batch.Columns.Add("StateId", typeof(long));
        batch.Columns.Add("TransDossId", typeof(long));
    }
}
