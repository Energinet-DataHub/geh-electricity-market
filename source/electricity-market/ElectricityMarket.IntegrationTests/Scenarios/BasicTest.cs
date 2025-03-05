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

using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollectionFixture))]
public class BasicTest : ScenarioBase
{
    public BasicTest(ScenarioTestFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task SimpleTest()
    {
        await RunWithinTransactionWithRollbackAsync(
            () =>
            {
                // Get the DbContext factory from the service provider
                var contextFactory = GetService<IDbContextFactory<ElectricityMarketDatabaseContext>>();

                // Create a DbContext instance
                using var context = contextFactory.CreateDbContext();

                // Query the ImportedTransactions table
                var transactionExists = context.ImportedTransactions.Any();

                // Assert that records exist (the CSV import should have added some)
                Assert.True((bool)transactionExists, "Expected imported transactions to exist in the database");

                // You could also check specific records if needed
                var firstTransaction = context.ImportedTransactions.FirstOrDefault();
                Assert.NotNull(firstTransaction);
            },
            "gold-import.csv");
    }
}
