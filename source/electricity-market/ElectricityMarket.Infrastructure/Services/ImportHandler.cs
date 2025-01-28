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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class ImportHandler : IImportHandler
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly IQuarantineZone _quarantineZone;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly IEnumerable<ITransactionImporter> _transactionImporters;

    public ImportHandler(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        IQuarantineZone quarantineZone,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        IEnumerable<ITransactionImporter> transactionImporters)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _quarantineZone = quarantineZone;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _transactionImporters = transactionImporters;
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        const int limit = 1_000;

        do
        {
            var transaction = await _electricityMarketDatabaseContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (transaction.ConfigureAwait(false))
            {
                var importState = await _electricityMarketDatabaseContext.ImportStates.SingleAsync(cancellationToken).ConfigureAwait(false);

                if (!importState.Enabled)
                {
                    return;
                }

                var offset = importState.Offset;

                var results = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(
                    DatabricksStatement.FromRawSql(
                        $"""
                         SELECT *
                         FROM migrations_electricity_market.electricity_market_metering_points_view_v2
                         WHERE btd_business_trans_doss_id > {importState.Offset} and
                         (
                            metering_point_id = '571313180401280099' or
                            metering_point_id = '571313180401280556' or
                            metering_point_id = '571313180401280044' or
                            metering_point_id = '571313180401280792' or
                            metering_point_id = '571313180401280815'
                         )
                         order by btd_business_trans_doss_id, metering_point_state_id
                         LIMIT {limit} OFFSET 0
                         """).Build(),
                    cancellationToken);

                await foreach (var record in results)
                {
                    var meteringPointTransaction = (MeteringPointTransaction)CreateMeteringPointTransaction(record);

                    offset = meteringPointTransaction.BusinessTransactionDosId;

                    if (await _quarantineZone.IsQuarantinedAsync(meteringPointTransaction).ConfigureAwait(false))
                    {
                        await _quarantineZone.QuarantineAsync(meteringPointTransaction, "Previously quarantined").ConfigureAwait(false);
                        continue;
                    }

                    var meteringPoint = await GetAsync(meteringPointTransaction.Identification).ConfigureAwait(false) ??
                                        await CreateAsync(meteringPointTransaction.Identification).ConfigureAwait(false);

                    await RunImportChainAsync(meteringPoint, meteringPointTransaction).ConfigureAwait(false);
                }

                if (offset == importState.Offset)
                {
                    return;
                }

                importState.Offset = offset;

                await _electricityMarketDatabaseContext.SaveChangesAsync().ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    private static MeteringPointTransaction CreateMeteringPointTransaction(dynamic record)
    {
        return new MeteringPointTransaction(
            record.metering_point_id,
            record.valid_from_date,
            record.valid_to_date ?? DateTimeOffset.MaxValue,
            record.dh3_created,
            record.metering_grid_area_id,
            record.metering_point_state_id,
            record.btd_business_trans_doss_id,
            record.physical_status_of_mp,
            record.type_of_mp,
            record.sub_type_of_mp,
            record.energy_timeseries_measure_unit);
    }

    private async Task RunImportChainAsync(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        var handled = false;

        foreach (var transactionImporter in _transactionImporters)
        {
            var result = await transactionImporter.ImportAsync(meteringPoint, meteringPointTransaction).ConfigureAwait(false);

            if (result.Status == TransactionImporterResultStatus.Error)
            {
                await _quarantineZone.QuarantineAsync(meteringPointTransaction, result.Message).ConfigureAwait(false);
                return;
            }

            handled |= result.Status == TransactionImporterResultStatus.Handled;
        }

        if (!handled)
        {
            await _quarantineZone.QuarantineAsync(meteringPointTransaction, "Unhandled").ConfigureAwait(false);
        }
    }

    private async Task<MeteringPointEntity?> GetAsync(string identification)
    {
        return await _electricityMarketDatabaseContext.MeteringPoints
            .FirstOrDefaultAsync(x => x.Identification == identification)
            .ConfigureAwait(false);
    }

    private async Task<MeteringPointEntity> CreateAsync(string identification)
    {
        var entity = new MeteringPointEntity
        {
            Identification = identification,
        };

        _electricityMarketDatabaseContext.MeteringPoints.Add(entity);

        await _electricityMarketDatabaseContext.SaveChangesAsync().ConfigureAwait(false);

        return entity;
    }
}
