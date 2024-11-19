﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.ElectricityMarket.Integration.Options;
using Energinet.DataHub.ElectricityMarket.Integration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Integration.Extensions.DependencyInjection;

public static class ModuleExtensions
{
    public static IServiceCollection AddElectricityMarketModule(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
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

        return services;
    }
}
