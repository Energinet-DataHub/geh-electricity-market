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

using ElectricityMarket.ImportOrchestrator.Orchestration.Activities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using RetryContext = Microsoft.DurableTask.RetryContext;

namespace ElectricityMarket.ImportOrchestrator.Orchestration;

#pragma warning disable CA2007
public sealed class InitialImportOrchestrator
{
    [Function(nameof(OrchestrateInitialImportAsync))]
    public async Task OrchestrateInitialImportAsync(
        [OrchestrationTrigger] TaskOrchestrationContext orchestrationContext,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(orchestrationContext);
        ArgumentNullException.ThrowIfNull(executionContext);

        await ImportGoldModelAsync(orchestrationContext);
        await ImportRelationalModelAsync(orchestrationContext);
    }

    private static async Task ImportGoldModelAsync(TaskOrchestrationContext orchestrationContext)
    {
        var cutoff = await orchestrationContext.CallActivityAsync<long>(nameof(FindCutoffActivity));

        await orchestrationContext.CallActivityAsync(nameof(ImportGoldModelActivity), new ImportGoldModelActivityInput
        {
            Cutoff = cutoff,
        });
    }

    private static async Task ImportRelationalModelAsync(TaskOrchestrationContext orchestrationContext)
    {
        var numberOfMeteringPoints = await orchestrationContext.CallActivityAsync<int>(nameof(FindNumberOfUniqueMeteringPointsActivity));

        var batchSize = 300_000;
        var activityCount = (int)Math.Ceiling(numberOfMeteringPoints / (double)batchSize);

        var tasks = Enumerable.Range(0, activityCount)
            .Select(i => orchestrationContext.CallActivityAsync(
                nameof(ImportRelationalModelActivity),
                new ImportRelationalModelActivityInput { Skip = i * batchSize, Take = batchSize, },
                TaskOptions.FromRetryHandler(HandleDataSourceExceptions)));

        await Task.WhenAll(tasks);

        static bool HandleDataSourceExceptions(RetryContext context)
        {
            return context.LastAttemptNumber <= 3;
        }
    }
}
#pragma warning restore CA2007
