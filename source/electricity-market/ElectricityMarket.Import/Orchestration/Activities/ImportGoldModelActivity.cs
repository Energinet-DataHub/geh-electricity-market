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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ElectricityMarket.Import.Orchestration.Activities;

public sealed class ImportGoldModelActivity
{
    private readonly ILogger<ImportGoldModelActivity> _logger;
    private readonly IGoldenImportHandler _goldenImportHandler;

    public ImportGoldModelActivity(ILogger<ImportGoldModelActivity> logger, IGoldenImportHandler goldenImportHandler)
    {
        _logger = logger;
        _goldenImportHandler = goldenImportHandler;
    }

    [Function(nameof(ImportGoldModelActivity))]
    public async Task RunAsync([ActivityTrigger] ActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var sw = Stopwatch.StartNew();

        // todo - just move logic to this class?
        await _goldenImportHandler.ImportAsync(input.MaxTransDossId).ConfigureAwait(false);

        _logger.LogWarning("Gold model imported in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
    }

#pragma warning disable CA1034
    public class ActivityInput
    {
        public long MaxTransDossId { get; set; }
    }
#pragma warning restore CA1034
}
