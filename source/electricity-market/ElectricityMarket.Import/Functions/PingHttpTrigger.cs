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

using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ElectricityMarket.Import.Functions;

internal sealed class PingHttpTrigger
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;

    public PingHttpTrigger(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
    }

    [Function(nameof(PingAsync))]
    public async Task<IActionResult> PingAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ping")]
        HttpRequest req)
    {
        var electricityMarketDatabaseContext = (ElectricityMarketDatabaseContext)_electricityMarketDatabaseContext;

        return new OkObjectResult($"""
                                   database connected: {await electricityMarketDatabaseContext.Database.CanConnectAsync().ConfigureAwait(false)}
                                   databricks connected: {await CanConnectToDatabricksAsync().ConfigureAwait(false)}
                                   """);
    }

    private async Task<bool> CanConnectToDatabricksAsync()
    {
        try
        {
            var results = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(DatabricksStatement.FromRawSql(
                """
                SELECT btd_business_trans_doss_id
                FROM migrations_electricity_market.electricity_market_metering_points_view_v2
                LIMIT 1 OFFSET 0
                """).Build());

            await foreach (var unused in results)
            {
                unused.ToString();
            }

            return true;
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }
}
