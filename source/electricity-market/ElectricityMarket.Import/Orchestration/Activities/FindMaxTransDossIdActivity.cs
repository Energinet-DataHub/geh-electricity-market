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
using Microsoft.Azure.Functions.Worker;

namespace ElectricityMarket.Import.Orchestration.Activities;

public sealed class FindMaxTransDossIdActivity
{
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;

    public FindMaxTransDossIdActivity(DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
    }

    [Function(nameof(FindMaxTransDossIdActivity))]
    public async Task<long> RunAsync([ActivityTrigger] NoInput input)
    {
        var result = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(
            DatabricksStatement.FromRawSql(
                """
                SELECT max(btd_business_trans_doss_id) as trans_dos_id FROM migrations_electricity_market.electricity_market_metering_points_view_v2
                """).Build()).ConfigureAwait(false);

        await foreach (var r in result)
        {
            return r.trans_dos_id;
        }

        throw new InvalidOperationException();
    }
}
