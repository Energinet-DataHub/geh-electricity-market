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

using ElectricityMarket.WebAPI.Security;
using Energinet.DataHub.ElectricityMarket.Application;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.RevisionLog.Integration;
using NodaTime;

namespace ElectricityMarket.WebAPI.Extensions.DependencyInjection;

public static class ElectricityMarketWebApiModuleExtensions
{
    private static readonly Guid _systemGuid = Guid.Parse("6A177C9D-4914-4607-BA5A-517C142B7F1F");

    public static IServiceCollection AddElectricityMarketWebApiModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddElectricityMarketModule(true);
        services.AddElectricityMarketDatabricksModule(configuration);

        // TODO: Move to package.
        services.RegisterEndpointAuthorizationContext();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ApplicationAssemblyReference>();
        });

        AddHealthChecks(services);

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDbContextCheck<ElectricityMarketDatabaseContext>();
    }

    private static void RegisterEndpointAuthorizationContext(this IServiceCollection services)
    {
        services.AddScoped<IEndpointAuthorizationContext, EndpointAuthorizationContext>();
        services.AddScoped<IEndpointAuthorizationLogger>(x =>
        {
            var revisionClient = x.GetRequiredService<IRevisionLogClient>();
            var httpContextAccessor = x.GetRequiredService<IHttpContextAccessor>();
            var clock = x.GetRequiredService<IClock>();
            return new EndpointAuthorizationLogger(httpContextAccessor, log =>
            {
                var logEntry = new RevisionLogEntry(
                    Guid.NewGuid(),
                    _systemGuid,
                    log.Activity,
                    clock.GetCurrentInstant(),
                    log.Endpoint,
                    log.RequestId.ToString(),
                    affectedEntityType: log.EntityType,
                    affectedEntityKey: log.EntityKey);

                return revisionClient.LogAsync(logEntry);
            });
        });
    }
}
