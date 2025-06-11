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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.Azure.Functions.Worker;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

internal sealed class ImportRelationalModelActivity
{
    public const string ActivityName = "ImportRelationalModelActivityV9";

    private readonly IBulkImporter _bulkImporter;

    public ImportRelationalModelActivity(IBulkImporter bulkImporter)
    {
        _bulkImporter = bulkImporter;
    }

    [Function(ActivityName)]
    public Task RunAsync([ActivityTrigger] ImportRelationalModelActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return _bulkImporter.RunAsync(input.Skip, input.Take);
    }
}
