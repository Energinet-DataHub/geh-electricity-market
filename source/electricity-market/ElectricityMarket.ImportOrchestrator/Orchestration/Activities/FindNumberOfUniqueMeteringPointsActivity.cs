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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class FindNumberOfUniqueMeteringPointsActivity
{
    private readonly ElectricityMarketDatabaseContext _databaseContext;

    public FindNumberOfUniqueMeteringPointsActivity(ElectricityMarketDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    [Function(nameof(FindNumberOfUniqueMeteringPointsActivity))]
    public async Task<int> RunAsync([ActivityTrigger] NoInput input)
    {
        return await _databaseContext.ImportedTransactions
            .Select(x => x.metering_point_id)
            .Distinct()
            .CountAsync()
            .ConfigureAwait(false);
    }
}
