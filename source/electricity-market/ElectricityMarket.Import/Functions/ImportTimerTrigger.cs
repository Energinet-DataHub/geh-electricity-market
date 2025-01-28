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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;

namespace ElectricityMarket.Import.Functions;

internal sealed class ImportTimerTrigger
{
    private readonly IImportHandler _importHandler;

    public ImportTimerTrigger(IImportHandler importHandler)
    {
        _importHandler = importHandler;
    }

    [Function(nameof(ImportAsync))]
    public async Task ImportAsync(
        [TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
        TimerInfo timer,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(28));

        try
        {
            await _importHandler.ImportAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }
}
