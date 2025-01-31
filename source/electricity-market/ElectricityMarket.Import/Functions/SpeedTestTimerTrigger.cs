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

using System.Diagnostics;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace ElectricityMarket.ImportOrchestrator.Functions;

internal sealed class SpeedTestTimerTrigger
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly IGoldenImportHandler _goldenImportHandler;

    public SpeedTestTimerTrigger(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        IGoldenImportHandler goldenImportHandler)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _goldenImportHandler = goldenImportHandler;
    }

    [Function(nameof(SpeedTestAsync))]
    public async Task SpeedTestAsync(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
        TimerInfo timer,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);

        var importState = await _electricityMarketDatabaseContext
            .SpeedTestImportEntities
            .SingleAsync()
            .ConfigureAwait(false);

        if (!importState.Enabled)
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        await _goldenImportHandler.ImportAsync().ConfigureAwait(false);

        importState.Enabled = false;
        importState.Offset = (int)sw.Elapsed.TotalSeconds;

        await _electricityMarketDatabaseContext
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }
}
