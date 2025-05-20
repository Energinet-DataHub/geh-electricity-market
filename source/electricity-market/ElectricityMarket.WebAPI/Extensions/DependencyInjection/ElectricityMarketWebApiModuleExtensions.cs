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

using Energinet.DataHub.ElectricityMarket.Application;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Authorization.Extensions;
using Energinet.DataHub.RevisionLog.Integration;
using NodaTime;

namespace ElectricityMarket.WebAPI.Extensions.DependencyInjection;

public static class ElectricityMarketWebApiModuleExtensions
{
    public static IServiceCollection AddElectricityMarketWebApiModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddElectricityMarketModule(true);
        services.AddElectricityMarketDatabricksModule(configuration);

        services
            .AddAuthorizationVerifyModule()
            .AddEndpointAuthorizationModule(serviceProvider =>
            {
                var revisionLogClient = serviceProvider.GetRequiredService<IRevisionLogClient>();
                var clock = serviceProvider.GetRequiredService<IClock>();
                return log => revisionLogClient.LogAsync(new RevisionLogEntry(
                    Guid.NewGuid(),
                    SubsystemInformation.Id,
                    log.Activity,
                    clock.GetCurrentInstant(),
                    log.Endpoint,
                    log.RequestId.ToString(),
                    affectedEntityType: log.EntityType,
                    affectedEntityKey: log.EntityKey));
            });

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
}
