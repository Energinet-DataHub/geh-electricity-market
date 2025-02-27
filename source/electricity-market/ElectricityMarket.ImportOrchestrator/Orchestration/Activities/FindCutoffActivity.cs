﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Diagnostics;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class FindCutoffActivity
{
    private readonly ILogger<FindCutoffActivity> _logger;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;

    public FindCutoffActivity(ILogger<FindCutoffActivity> logger, DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        _logger = logger;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
    }

    [Function(nameof(FindCutoffActivity))]
    public async Task<long> RunAsync([ActivityTrigger] NoInput input)
    {
        var sw = Stopwatch.StartNew();

        var result = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(
            DatabricksStatement.FromRawSql(
                """
                SELECT MAX(btd_trans_doss_id) AS cutoff FROM migrations_electricity_market.electricity_market_metering_points_view_v3
                """).Build()).ConfigureAwait(false);

        await foreach (var r in result)
        {
            _logger.LogWarning("Cutoff fetched in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
            return r.cutoff;
        }

        throw new InvalidOperationException("No rows found in silver model");
    }
}
