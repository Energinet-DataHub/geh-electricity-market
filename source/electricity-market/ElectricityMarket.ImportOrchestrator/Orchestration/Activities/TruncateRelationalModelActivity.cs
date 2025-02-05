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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class TruncateRelationalModelActivity
{
    private readonly ILogger<TruncateRelationalModelActivity> _logger;
    private readonly IElectricityMarketDatabaseContext _databaseContext;

    public TruncateRelationalModelActivity(ILogger<TruncateRelationalModelActivity> logger, IElectricityMarketDatabaseContext databaseContext)
    {
        _logger = logger;
        _databaseContext = databaseContext;
    }

    [Function(nameof(TruncateRelationalModelActivity))]
    public async Task RunAsync([ActivityTrigger] NoInput input)
    {
        var sw = Stopwatch.StartNew();

        await _databaseContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM [electricitymarket].[EnergySupplyPeriod];
            DELETE FROM [electricitymarket].[CommercialRelation];
            DELETE FROM [electricitymarket].[MeteringPointPeriod];
            DELETE FROM [electricitymarket].[MeteringPoint];
            """).ConfigureAwait(false);

        _logger.LogWarning("Relational model truncated in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
    }
}
