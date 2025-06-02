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

using ElectricityMarket.ImportOrchestrator.Monitor;
using ElectricityMarket.ImportOrchestrator.Orchestration.Activities;
using ElectricityMarket.ImportOrchestrator.Services;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution.Diagnostics.HealthChecks;
using Energinet.DataHub.ElectricityMarket.Application;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectricityMarket.ImportOrchestrator.Extensions.DependencyInjection;

internal static class ElectricityMarketImportOrchestratorModuleExtensions
{
    public static IServiceCollection AddElectricityMarketImportOrchestratorModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddElectricityMarketModule(configuration);

        services
            .AddOptions<DatabricksCatalogOptions>()
            .BindConfiguration(DatabricksCatalogOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddDatabricksSqlStatementExecution(configuration.GetSection("Databricks"));

        Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.DatabricksSqlStatementExecutionExtensions
            .AddDatabricksSqlStatementExecution(services, configuration.GetSection("Databricks"));

        services.AddScoped<FindCutoffActivity>();
        services.AddScoped<IImportStateService, ImportStateService>();
        services.AddScoped<IMeteringPointImporter, MeteringPointImporter>();
        services.AddScoped<IImportedTransactionModelReader, ImportedTransactionModelReader>();
        services.AddScoped<IRelationalModelWriter, RelationalModelWriter>();
        services.AddScoped<IBulkImporter, BulkImporter>();
        services.AddScoped<IStreamingImporter, StreamingImporter>();
        services.AddScoped<IGoldenStreamingImporter, GoldenStreamingImporter>();
        services.AddScoped<IDatabricksStreamingImporter, DatabricksStreamingImporter>();
        services.AddScoped<Func<IDatabricksStreamingImporter>>(scope => scope.GetRequiredService<IDatabricksStreamingImporter>);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ApplicationAssemblyReference>();
        });

        AddHealthChecks(services, configuration);

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<HealthCheckEndpoint>();

        var healthCheckBuilder = services
            .AddHealthChecks()
            .AddDbContextCheck<ElectricityMarketDatabaseContext>();

        if (configuration.IsSettingEnabled("EnableDatabricksHealthCheck"))
        {
            healthCheckBuilder.AddDatabricksSqlStatementApiHealthCheck();
        }
    }
}
