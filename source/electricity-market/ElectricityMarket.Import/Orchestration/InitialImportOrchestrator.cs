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
using Microsoft.Extensions.Logging;

namespace ElectricityMarket.Import.Orchestration;

#pragma warning disable CA2007
public sealed class InitialImportOrchestrator
{
    private readonly ILogger<InitialImportOrchestrator> _logger;

    public InitialImportOrchestrator(ILogger<InitialImportOrchestrator> logger)
    {
        _logger = logger;
    }

    [Function(nameof(OrchestrateInitalImportAsync))]
    public async Task OrchestrateInitalImportAsync(
        [OrchestrationTrigger] TaskOrchestrationContext orchestrationContext,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(orchestrationContext);
        ArgumentNullException.ThrowIfNull(executionContext);

        var maxTransDosId = await orchestrationContext.CallActivityAsync<long>(nameof(FindMaxTransDossIdActivity));

        await orchestrationContext.CallActivityAsync(nameof(TruncateGoldModelActivity));
        await orchestrationContext.CallActivityAsync(nameof(ImportGoldModelActivity));
        // await orchestrationContext.CallActivityAsync(nameof(TruncateRelationalModelActivity));
        // var numberOfMeteringPoints = await orchestrationContext.CallActivityAsync<int>(nameof(FindNumberOfUniqueMeteringPointsActivity));
        // var cores = 1;
        // var conservativeBatcSize = numberOfMeteringPoints / cores;
        // var offsets = Enumerable.Range(0, cores)
        //     .Select(i =>
        //         (skip: i * conservativeBatcSize, take: i == cores - 1 ? numberOfMeteringPoints - (i * conservativeBatcSize) : conservativeBatcSize)).ToList();
        //
        // var dataSourceExceptionHandler = TaskOptions.FromRetryHandler(_ => HandleDataSourceExceptions());
        //
        // var tasks = offsets.Select(offset => orchestrationContext.CallActivityAsync(
        //     nameof(ImportRelationalModelActivity),
        //     new ImportRelationalModelActivity.ActivityInput
        //     {
        //         MeteringPointRange = new ImportRelationalModelActivity.ActivityInput.Page
        //         {
        //             Skip = offset.skip,
        //             Take = offset.take,
        //         },
        //     },
        //     dataSourceExceptionHandler));
        //
        // await Task.WhenAll(tasks);
    }

    private static bool HandleDataSourceExceptions()
    {
        return true;
    }
}
#pragma warning restore CA2007
