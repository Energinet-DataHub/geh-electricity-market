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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class ImportHandler : IImportHandler
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly IImportStateRepository _importStateRepository;
    private readonly IMeteringPointRepository _meteringPointRepository;

    public ImportHandler(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        IImportStateRepository importStateRepository,
        IMeteringPointRepository meteringPointRepository)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _importStateRepository = importStateRepository;
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        const int limit = 1024;

        do
        {
            var transaction = await _electricityMarketDatabaseContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (transaction.ConfigureAwait(false))
            {
                var importState = await _importStateRepository.GetImportStateAsync().ConfigureAwait(false);

                if (!importState.Enabled)
                {
                    return;
                }

                // temp: test write permissions
                await _importStateRepository.UpdateImportStateAsync(importState).ConfigureAwait(false);

                var offset = importState.Offset;

                var results = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(
                    DatabricksStatement.FromRawSql(
                        $"""
                         SELECT *
                         FROM migrations_electricity_market.electricity_market_metering_points_view_v2
                         WHERE btd_business_trans_doss_id > {importState.Offset}
                         ORDER BY btd_business_trans_doss_id
                         LIMIT {limit} OFFSET 0
                         """).Build(),
                    cancellationToken);

                await foreach (var record in results)
                {
                    var identification = (string)record.metering_point_id;

                    _ = await _meteringPointRepository.GetAsync(new MeteringPointIdentification(identification)).ConfigureAwait(false) ??
                        await _meteringPointRepository.CreateAsync(new MeteringPointIdentification(identification)).ConfigureAwait(false);

                    offset = (long)record.btd_business_trans_doss_id;
                }

                if (offset == importState.Offset)
                {
                    return;
                }

                importState.Offset = offset;

                // update import state
                await _importStateRepository.UpdateImportStateAsync(importState).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        while (!cancellationToken.IsCancellationRequested);
    }
}
