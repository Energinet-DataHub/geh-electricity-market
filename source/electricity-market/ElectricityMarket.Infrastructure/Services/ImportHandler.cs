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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class ImportHandler : IImportHandler
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly IEnumerable<ITransactionImporter> _transactionImporters;

    public ImportHandler(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        IEnumerable<ITransactionImporter> transactionImporters)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _transactionImporters = transactionImporters;
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        const int limit = 1024;

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
                         WHERE metering_point_state_id > {importState.Offset}
                         ORDER BY metering_point_state_id
                         LIMIT {limit} OFFSET 0
                         """).Build(),
                    cancellationToken);

                await foreach (var record in results)
                {
                    var identification = (string)record.metering_point_id;

                    var meteringPoint = await GetAsync(identification).ConfigureAwait(false) ??
                                        Create(identification);

                    var handled = false;

                    foreach (var transactionImporter in _transactionImporters)
                    {
                        handled |= await transactionImporter.ImportAsync(meteringPoint).ConfigureAwait(false);
                    }

                    if (!handled)
                    {
                        // quarantine record
                        throw new InvalidOperationException("Unhandled transaction");
                    }

                    offset = (long)record.metering_point_state_id;
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

    private async Task<MeteringPointEntity?> GetAsync(string identification)
    {
        return await _electricityMarketDatabaseContext.MeteringPoints
            .FirstOrDefaultAsync(x => x.Identification == identification)
            .ConfigureAwait(false);
    }

    private MeteringPointEntity Create(string identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = new MeteringPointEntity
        {
            Identification = identification,
        };

        _electricityMarketDatabaseContext.MeteringPoints.Add(entity);

        return entity;
    }
}
