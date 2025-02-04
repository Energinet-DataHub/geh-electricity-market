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

using ElectricityMarket.Import.Orchestration.Activities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using RetryContext = Microsoft.DurableTask.RetryContext;

namespace ElectricityMarket.Import.Orchestration;

#pragma warning disable CA2007
public sealed class InitialImportOrchestrator
{
    [Function(nameof(OrchestrateInitalImportAsync))]
    public async Task OrchestrateInitalImportAsync(
        [OrchestrationTrigger] TaskOrchestrationContext orchestrationContext,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(orchestrationContext);
        ArgumentNullException.ThrowIfNull(executionContext);

        var maxTransDosId = await orchestrationContext.CallActivityAsync<long>(nameof(FindMaxTransDossIdActivity));

        await orchestrationContext.CallActivityAsync(nameof(TruncateGoldModelActivity));

        await orchestrationContext.CallActivityAsync(nameof(ImportGoldModelActivity), new ImportGoldModelActivity.ActivityInput
        {
            MaxTransDossId = maxTransDosId,
        });
        await orchestrationContext.CallActivityAsync(nameof(TruncateRelationalModelActivity));

        var numberOfMeteringPoints = await orchestrationContext.CallActivityAsync<int>(nameof(FindNumberOfUniqueMeteringPointsActivity));
        var batchSize = 50_000;
        var activityCount = (int)Math.Ceiling(numberOfMeteringPoints / (double)batchSize);
        var offsets = Enumerable.Range(0, activityCount)
            .Select(i =>
                (skip: i * batchSize, take: i == activityCount - 1 ? numberOfMeteringPoints - (i * batchSize) : batchSize)).ToList();

        var dataSourceExceptionHandler = TaskOptions.FromRetryHandler(HandleDataSourceExceptions);

        var tasks = offsets.Select(offset => orchestrationContext.CallActivityAsync(
            nameof(ImportRelationalModelActivity),
            new ImportRelationalModelActivity.ActivityInput
            {
                MeteringPointRange = new ImportRelationalModelActivity.ActivityInput.Page
                {
                    Skip = offset.skip,
                    Take = offset.take,
                },
            },
            dataSourceExceptionHandler));

        await Task.WhenAll(tasks);
    }

    private static bool HandleDataSourceExceptions(RetryContext context)
    {
        return context.LastAttemptNumber < 20;
    }
}
#pragma warning restore CA2007
