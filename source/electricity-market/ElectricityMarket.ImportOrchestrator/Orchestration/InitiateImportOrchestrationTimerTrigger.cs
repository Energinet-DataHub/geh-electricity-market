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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore;

namespace ElectricityMarket.ImportOrchestrator.Orchestration;

public sealed class InitiateImportOrchestrationTimerTrigger
{
    private readonly ElectricityMarketDatabaseContext _electricityMarketDatabaseContext;

    public InitiateImportOrchestrationTimerTrigger(ElectricityMarketDatabaseContext electricityMarketDatabaseContext)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
    }

    [Function(nameof(InitiateImportOrchestrationAsync))]
    public async Task InitiateImportOrchestrationAsync(
        [TimerTrigger("0 */1 * * * *", RunOnStartup = true)]
        TimerInfo timer,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(client);

        var importState = await _electricityMarketDatabaseContext
            .ImportStates
            .SingleAsync()
            .ConfigureAwait(false);

        if (!importState.Enabled)
        {
            return;
        }

        importState.Enabled = false;

        await client.ScheduleNewOrchestrationInstanceAsync(nameof(InitialImportOrchestrator.OrchestrateInitialImportAsync)).ConfigureAwait(false);

        await _electricityMarketDatabaseContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
