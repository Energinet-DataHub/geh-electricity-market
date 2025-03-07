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
using System.Transactions;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;

public sealed class ScenarioTestFixture : IAsyncLifetime
{
    private readonly ElectricityMarketDatabaseFixture _databaseFixture = new();
    private ServiceProvider? _serviceProvider;
    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Service provider is not initialized. Ensure InitializeAsync has been called.");
    public ElectricityMarketDatabaseManager DatabaseManager => _databaseFixture.DatabaseManager;

    public async Task InitializeAsync()
    {
        // Build service collection and register dependencies
        await _databaseFixture.InitializeAsync();
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        // Register options
        services.AddOptions();
        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = DatabaseManager.ConnectionString;
        });
        services.AddSingleton(configuration);

        // Add logging services
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Register database context factory
        services.AddDbContextFactory<ElectricityMarketDatabaseContext>((p, o) =>
        {
            o.UseSqlServer(DatabaseManager.ConnectionString, sqlOptions =>
                {
                    sqlOptions.UseNodaTime();
                })
                .LogTo(_ => { }, [DbLoggerCategory.Database.Command.Name], LogLevel.None);
        });

        // Register dependencies
        services.AddScoped<ICsvImporter, CsvImporter>();
        services.AddScoped<IImportedTransactionRepository, ImportedTransactionRepository>();
        services.AddScoped<IRelationalModelPrinter, RelationalModelPrinter>();
        services.AddScoped<IMeteringPointImporter, MeteringPointImporter>();
        services.AddScoped<IImportedTransactionModelReader, ImportedTransactionModelReader>();
        services.AddScoped<IRelationalModelWriter, RelationalModelWriter>();
        services.AddScoped<IRelationalModelPrinter, RelationalModelPrinter>();
        services.AddScoped<IStreamingImporter, StreamingImporter>();

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

        await _databaseFixture.DisposeAsync();
    }

    private IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(
                    $"{nameof(DatabaseOptions)}:{nameof(DatabaseOptions.ConnectionString)}",
                    DatabaseManager.ConnectionString)
            })
            .AddEnvironmentVariables()
            .Build();
    }
}
