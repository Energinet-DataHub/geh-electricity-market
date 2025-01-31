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

using ElectricityMarket.Import.Monitor;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution.Diagnostics.HealthChecks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectricityMarket.Import.Extensions.DependencyInjection;

public static class ElectricityMarketImportModuleExtensions
{
    public static IServiceCollection AddElectricityMarketImportModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddElectricityMarketModule();

        services.AddDatabricksSqlStatementExecution(configuration.GetSection("Databricks"));

        // Services
        services.AddScoped<IImportHandler, ImportHandler>();
        services.AddScoped<IGoldenImportHandler, GoldenImportHandler>();
        services.AddScoped<IQuarantineZone, QuarantineZone>();

        // importers
        services.AddScoped<ITransactionImporter, MeteringPointPeriodImporter>();
        services.AddScoped<ITransactionImporter, CommercialRelationImporter>();

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
