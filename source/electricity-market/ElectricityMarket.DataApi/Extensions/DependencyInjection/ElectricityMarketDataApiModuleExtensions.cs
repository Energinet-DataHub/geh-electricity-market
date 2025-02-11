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
using Energinet.DataHub.ElectricityMarket.Hosts.DataApi.Monitor;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.ElectricityMarket.Hosts.DataApi.Extensions.DependencyInjection;

public static class ElectricityMarketDataApiModuleExtensions
{
    public static IServiceCollection AddElectricityMarketDataApiModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ApplicationAssemblyReference>();
        });

        services.AddElectricityMarketModule();

        AddHealthChecks(services);

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services
            .AddScoped<HealthCheckEndpoint>()
            .AddHealthChecks()
            .AddDbContextCheck<ElectricityMarketDatabaseContext>();
    }
}
