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

using ElectricityMarket.ImportOrchestrator.Services;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;

namespace ElectricityMarket.ImportOrchestrator.Orchestration;

public sealed class ContinuousImportTimerTrigger
{
    private readonly IImportStateService _importStateService;
    private readonly IDatabricksStreamingImporter _databricksStreamingImporter;

    public ContinuousImportTimerTrigger(
        IImportStateService importStateService,
        IDatabricksStreamingImporter databricksStreamingImporter)
    {
        _importStateService = importStateService;
        _databricksStreamingImporter = databricksStreamingImporter;
    }

    [Function(nameof(ContinuousImportTimerTrigger))]
    public async Task ImportAsync(
        [TimerTrigger("0 */1 * * * *", RunOnStartup = true)]
        TimerInfo timer,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (await _importStateService.IsStreamingImportEnabledAsync().ConfigureAwait(false))
        {
            await _databricksStreamingImporter.ImportAsync().ConfigureAwait(false);
        }
        else if (await _importStateService.IsImportPendingAsync().ConfigureAwait(false))
        {
            await client
                .ScheduleNewOrchestrationInstanceAsync(nameof(InitialImportOrchestrator.OrchestrateInitialImportAsync))
                .ConfigureAwait(false);

            await _importStateService.EnableBulkImportAsync().ConfigureAwait(false);
        }
    }
}
