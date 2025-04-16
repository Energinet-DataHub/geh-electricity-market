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

using System;
using System.Threading.Tasks;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Database;
using Energinet.DataHub.ElectricityMarket.DatabaseMigration.Helpers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;

public class ElectricityMarketContextDatabaseManager : SqlServerDatabaseManager<ElectricityMarketDatabaseContext>
{
    public ElectricityMarketContextDatabaseManager()
        : base("ElectricityMarket")
    {
    }

    public override ElectricityMarketDatabaseContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ElectricityMarketDatabaseContext>()
            .UseSqlServer(ConnectionString, options =>
            {
                options.UseNodaTime();
                options.EnableRetryOnFailure();
            });

        return new ElectricityMarketDatabaseContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Creates the database schema using database context.
    /// </summary>
    protected override async Task<bool> CreateDatabaseSchemaAsync(ElectricityMarketDatabaseContext context)
    {
      return await context.Database.EnsureCreatedAsync();
    }

    private static Func<string, bool> GetFilter()
    {
        return file =>
            file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
            file.Contains(".Scripts.LocalDB.", StringComparison.OrdinalIgnoreCase);
    }
}
