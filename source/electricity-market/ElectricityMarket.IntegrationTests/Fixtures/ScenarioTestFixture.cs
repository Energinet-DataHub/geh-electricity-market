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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;

public sealed class ScenarioTestFixture : IAsyncLifetime
{
    private readonly ElectricityMarketDbUpDatabaseFixture _dbUpDatabaseFixture = new();
    private ServiceProvider? _serviceProvider;

    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider is not initialized. Ensure InitializeAsync has been called.");
    public ElectricityMarketDbUpDatabaseManager DbUpDatabaseManager => _dbUpDatabaseFixture.DbUpDatabaseManager;

    public async Task InitializeAsync()
    {
        // Build service collection and register dependencies
        await _dbUpDatabaseFixture.InitializeAsync();
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        // Register options
        services.AddOptions();
        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = DbUpDatabaseManager.ConnectionString;
        });
        services.AddSingleton(configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await _dbUpDatabaseFixture.DisposeAsync();
    }

    private IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>(
                    $"{nameof(DatabaseOptions)}:{nameof(DatabaseOptions.ConnectionString)}",
                    DbUpDatabaseManager.ConnectionString),
            ])
            .AddEnvironmentVariables()
            .Build();
    }
}
