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
using Energinet.DataHub.ElectricityMarket.DatabaseMigration;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;

public class ElectricityMarketDatabaseManager : SqlServerDatabaseManager<ElectricityMarketDatabaseContext>
{
    public ElectricityMarketDatabaseManager()
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
    /// Creates the database schema using DbUp instead of a database context.
    /// </summary>
    protected override Task<bool> CreateDatabaseSchemaAsync(ElectricityMarketDatabaseContext context)
    {
        return Task.FromResult(CreateDatabaseSchema(context));
    }

    /// <summary>
    ///     Creates the database schema using DbUp instead of a database context.
    /// </summary>
    protected override bool CreateDatabaseSchema(ElectricityMarketDatabaseContext context)
    {
        var result = Upgrader.DatabaseUpgrade(ConnectionString);
        if (!result.Successful)
        {
#pragma warning disable CA2201
            throw new Exception("Database migration failed", result.Error);
#pragma warning restore CA2201
        }

        return true;
    }
}
