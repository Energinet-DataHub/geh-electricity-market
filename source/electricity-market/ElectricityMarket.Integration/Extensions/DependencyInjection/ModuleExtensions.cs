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

using System.Net.Http;
using Energinet.DataHub.ElectricityMarket.Integration.Options;
using Energinet.DataHub.ElectricityMarket.Integration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Integration.Extensions.DependencyInjection;

public static class ModuleExtensions
{
    public static IServiceCollection AddElectricityMarketModule(this IServiceCollection services)
    {
        services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
            .ValidateDataAnnotations();

        services
            .AddOptions<ApiClientOptions>()
            .BindConfiguration(nameof(ApiClientOptions))
            .ValidateDataAnnotations();

        services.AddDbContext<ElectricityMarketDatabaseContext>((s, o) =>
        {
            var dbSettings = s.GetRequiredService<IOptions<DatabaseOptions>>();
            o.UseSqlServer(dbSettings.Value.ConnectionString, builder =>
            {
                builder.UseNodaTime();
                builder.UseAzureSqlDefaults();
            });
        });

        services.AddHttpClient("ElectricityMarketClient", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ApiClientOptions>>();
            client.BaseAddress = options.Value.BaseUrl;
        });

        services.AddScoped<IElectricityMarketViews, ElectricityMarketViews>(s =>
        {
            var client = s.GetRequiredService<IHttpClientFactory>().CreateClient("ElectricityMarketClient");
            return new ElectricityMarketViews(s.GetRequiredService<ElectricityMarketDatabaseContext>(), client);
        });
        return services;
    }
}
