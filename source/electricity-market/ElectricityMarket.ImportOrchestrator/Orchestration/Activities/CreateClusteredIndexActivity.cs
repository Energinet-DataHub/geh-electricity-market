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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

internal sealed class CreateClusteredIndexActivity
{
    private readonly ElectricityMarketDatabaseContext _databaseContext;

    public CreateClusteredIndexActivity(ElectricityMarketDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    [Function(nameof(CreateClusteredIndexActivity))]
    public async Task RunAsync([ActivityTrigger] NoInput input)
    {
        await _databaseContext.Database
            .ExecuteSqlRawAsync("""
                                IF EXISTS (
                                    SELECT 1 
                                    FROM sys.indexes 
                                    WHERE name = 'IX_GoldenImport_btd_trans_doss_id'
                                )
                                BEGIN
                                    DROP INDEX IX_GoldenImport_btd_trans_doss_id ON [electricitymarket].[GoldenImport];
                                END
                                """)
            .ConfigureAwait(false);

        await _databaseContext.Database
            .ExecuteSqlRawAsync("CREATE CLUSTERED INDEX [IX_GoldenImport_btd_trans_doss_id] ON [electricitymarket].[GoldenImport] (metering_point_id, btd_trans_doss_id, metering_point_state_id)")
            .ConfigureAwait(false);
    }
}
