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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class SpeedTestImportHandler : ISpeedTestImportHandler
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;

    public SpeedTestImportHandler(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
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

        var offset = importState.Offset;
        var runningSum = 0L;

        var results = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(
            DatabricksStatement.FromRawSql(
                $"""
                 SELECT metering_point_id, valid_from_date, valid_to_date, dh3_created, metering_grid_area_id, metering_point_state_id, btd_business_trans_doss_id
                 FROM migrations_electricity_market.electricity_market_metering_points_view_v2
                 WHERE btd_business_trans_doss_id <= 339762452 
                 """).Build(),
            cancellationToken);

        await foreach (var record in results)
        {
            var meteringPointId = (string)record.metering_point_id;
            var validFrom = (DateTimeOffset)record.valid_from_date;
            var validTo = record.valid_to_date == null ? DateTimeOffset.MaxValue : (DateTimeOffset)record.valid_to_date;
            var createdDate = (DateTimeOffset)record.dh3_created;
            var gridArea = (string)record.metering_grid_area_id;
            var stateId = (long)record.metering_point_state_id;
            var transDossId = (long)record.btd_business_trans_doss_id;

            var entity = new SpeedTestGoldEntity
            {
                MeteringPointId = meteringPointId,
                ValidFrom = validFrom,
                ValidTo = validTo,
                CreatedDate = createdDate,
                GridArea = gridArea,
                StateId = stateId,
                TransDossId = transDossId
            };

            offset++;
            runningSum += entity.TransDossId;
        }

        if (offset == importState.Offset)
        {
            return;
        }

        importState.Enabled = false;
        importState.Offset = offset;

        await _electricityMarketDatabaseContext
            .SaveChangesAsync()
            .ConfigureAwait(false);

        if (runningSum > 10)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
    }
}
