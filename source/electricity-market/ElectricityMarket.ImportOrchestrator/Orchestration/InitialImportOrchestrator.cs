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

using ElectricityMarket.ImportOrchestrator.Orchestration.Activities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

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

        var cutoff = await orchestrationContext.CallActivityAsync<long>(nameof(FindCutoffActivity));

        // await ImportGoldModelAsync(orchestrationContext, cutoff);
        await ImportRelationalModelAsync(orchestrationContext);

        await orchestrationContext.CallActivityAsync(nameof(SwitchToStreamingActivity), new SwitchToStreamingActivityInput
        {
            Cutoff = cutoff
        });
    }

    private static async Task ImportGoldModelAsync(TaskOrchestrationContext orchestrationContext, long cutoff)
    {
        var itemsInOneHour = 30_000_000;
        var itemsInOneHourCount = (int)Math.Ceiling(cutoff / (double)itemsInOneHour);

        for (var i = 0; i < itemsInOneHourCount; i++)
        {
            var offset = i;
            var cutoffResponse = await orchestrationContext.CallActivityAsync<CutoffResponse>(
                nameof(RequestCutoffActivity),
                new RequestCutoffActivityInput
                {
                    CutoffFromInclusive = offset * itemsInOneHour,
                    CutoffToExclusive = Math.Min((offset + 1) * itemsInOneHour, cutoff)
                });

            var tasks = cutoffResponse
                .Chunks
                .Select(chunk => orchestrationContext.CallActivityAsync(
                    ImportGoldModelActivity.ActivityName,
                    new ImportGoldModelActivityInput
                    {
                        StatementId = cutoffResponse.StatementId,
                        Chunk = chunk
                    }));

            await Task.WhenAll(tasks);
        }
    }

    private static async Task ImportRelationalModelAsync(TaskOrchestrationContext orchestrationContext)
    {
        await orchestrationContext.CallActivityAsync(nameof(CreateGoldMpIdIndexActivity), TaskOptions.FromRetryHandler(HandleDataSourceExceptions));
        await orchestrationContext.CallActivityAsync(nameof(CreateGoldTransDossIdIndexActivity), TaskOptions.FromRetryHandler(HandleDataSourceExceptions));

        var numberOfMeteringPoints = await orchestrationContext.CallActivityAsync<int>(nameof(FindNumberOfUniqueMeteringPointsActivity));

        var batchSize = 300_000;
        var activityCount = (int)Math.Ceiling(numberOfMeteringPoints / (double)batchSize);

        var tasks = Enumerable.Range(0, activityCount)
            .Select(i => orchestrationContext.CallActivityAsync(
                ImportRelationalModelActivity.ActivityName,
                new ImportRelationalModelActivityInput { Skip = i * batchSize, Take = batchSize, },
                TaskOptions.FromRetryHandler(HandleDataSourceExceptions)));

        await Task.WhenAll(tasks);

        static bool HandleDataSourceExceptions(Microsoft.DurableTask.RetryContext context)
        {
            return context.LastAttemptNumber <= 3;
        }
    }
}
