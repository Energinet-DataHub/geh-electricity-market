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

namespace ElectricityMarket.ImportOrchestrator.Orchestration;

#pragma warning disable CA2007

internal sealed class InitialImportOrchestrator
{
    [Function(nameof(OrchestrateInitialImportAsync))]
    public async Task OrchestrateInitialImportAsync(
        [OrchestrationTrigger] TaskOrchestrationContext orchestrationContext,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(orchestrationContext);
        ArgumentNullException.ThrowIfNull(executionContext);

        var cutoff = await orchestrationContext.CallActivityAsync<long>(nameof(FindCutoffActivity));

        await ImportGoldModelAsync(orchestrationContext, cutoff);
        await ImportRelationalModelAsync(orchestrationContext);

        await orchestrationContext.CallActivityAsync(nameof(SwitchToStreamingActivity), new SwitchToStreamingActivityInput
        {
            Cutoff = cutoff
        });
    }

    private static bool RetryHandler(Microsoft.DurableTask.RetryContext context)
    {
        return context.LastAttemptNumber <= 3;
    }

    private static async Task ImportGoldModelAsync(TaskOrchestrationContext orchestrationContext, long cutoff)
    {
        var itemsInOneHour = 30_000_000;
        var itemsInOneHourCount = (int)Math.Ceiling(cutoff / (double)itemsInOneHour);

        CutoffResponse? pendingResponse = null;

        for (var i = 0; i < itemsInOneHourCount; i++)
        {
            pendingResponse = await CreateJobAsync(i, pendingResponse);
        }

        if (pendingResponse != null)
        {
            var tasks = pendingResponse
                .Chunks
                .Select(chunk => orchestrationContext.CallActivityAsync(
                    ImportGoldModelActivity.ActivityName,
                    new ImportGoldModelActivityInput
                    {
                        StatementId = pendingResponse.StatementId,
                        Chunk = chunk
                    }));

            await Task.WhenAll(tasks);
        }

        await orchestrationContext.CallActivityAsync(nameof(CreateClusteredIndexActivity), TaskOptions.FromRetryHandler(RetryHandler));

        async Task<CutoffResponse> CreateJobAsync(int offset, CutoffResponse? previousJob)
        {
            var jobTask = orchestrationContext.CallActivityAsync<CutoffResponse>(
                nameof(RequestCutoffActivity),
                new RequestCutoffActivityInput
                {
                    CutoffFromInclusive = offset * itemsInOneHour,
                    CutoffToExclusive = Math.Min((offset + 1) * itemsInOneHour, cutoff)
                },
                TaskOptions.FromRetryHandler(RetryHandler));

            if (previousJob != null)
            {
                var tasks = previousJob
                    .Chunks
                    .Select(chunk => orchestrationContext.CallActivityAsync(
                        ImportGoldModelActivity.ActivityName,
                        new ImportGoldModelActivityInput
                        {
                            StatementId = previousJob.StatementId,
                            Chunk = chunk
                        }));

                await Task.WhenAll(tasks);
            }

            return await jobTask;
        }
    }

    private static async Task ImportRelationalModelAsync(TaskOrchestrationContext orchestrationContext)
    {
        var numberOfMeteringPoints = await orchestrationContext.CallActivityAsync<int>(nameof(FindNumberOfUniqueMeteringPointsActivity));

        var batchSize = 100_000;
        var activityCount = (int)Math.Ceiling(numberOfMeteringPoints / (double)batchSize);

        foreach (var activityChunk in Enumerable.Range(0, activityCount).Chunk(8))
        {
            var tasks = activityChunk
                .Select(i => orchestrationContext.CallActivityAsync(
                    ImportRelationalModelActivity.ActivityName,
                    new ImportRelationalModelActivityInput { Skip = i * batchSize, Take = batchSize, },
                    TaskOptions.FromRetryHandler(RetryHandler)));

            await Task.WhenAll(tasks);
        }
    }
}
