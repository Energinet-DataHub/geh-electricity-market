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
using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Integration.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Integration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;

public sealed class ElectricityMarketIntegrationFixture : IAsyncLifetime
{
    internal ElectricityMarketIntegrationDatabaseManager DatabaseManager { get; } = new();

    public Task InitializeAsync()
    {
        return DatabaseManager.CreateDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return DatabaseManager.DeleteDatabaseAsync();
    }

    public AsyncServiceScope BeginScope()
    {
        return new ServiceCollection()
            .AddSingleton(BuildConfiguration(DatabaseManager.ConnectionString))
            .AddElectricityMarketModule()
            .BuildServiceProvider()
            .CreateAsyncScope();
    }

    private static IConfiguration BuildConfiguration(string connectionString)
    {
        return new ConfigurationBuilder().AddInMemoryCollection([
            new KeyValuePair<string, string?>($"{nameof(DatabaseOptions)}:{nameof(DatabaseOptions.ConnectionString)}", connectionString),
        ]).Build();
    }
}
