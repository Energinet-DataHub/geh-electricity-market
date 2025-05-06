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
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions.DependencyInjection;

public static class ElectricityMarketModuleExtensions
{
    public static IServiceCollection AddElectricityMarketModule(this IServiceCollection services)
    {
        services.AddOptions();

        services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddDbContext<ElectricityMarketDatabaseContext>((p, o) =>
        {
            var databaseOptions = p.GetRequiredService<IOptions<DatabaseOptions>>();
            o.UseSqlServer(databaseOptions.Value.ConnectionString, options =>
            {
                options.UseNodaTime();
                options.CommandTimeout(60 * 60 * 2);
            })
            .LogTo(_ => { }, [DbLoggerCategory.Database.Command.Name], Microsoft.Extensions.Logging.LogLevel.None);
        });

        services.AddDbContext<IMarketParticipantDatabaseContext, MarketParticipantDatabaseContext>((p, o) =>
        {
            var databaseOptions = p.GetRequiredService<IOptions<DatabaseOptions>>();
            o.UseSqlServer(databaseOptions.Value.ConnectionString, options =>
            {
                options.UseNodaTime();
            })
            .LogTo(_ => { }, [DbLoggerCategory.Database.Command.Name], Microsoft.Extensions.Logging.LogLevel.None);
        });

        services.AddDbContextFactory<ElectricityMarketDatabaseContext>(
            (p, o) =>
            {
                var databaseOptions = p.GetRequiredService<IOptions<DatabaseOptions>>();
                o.UseSqlServer(databaseOptions.Value.ConnectionString, options =>
                {
                    options.UseNodaTime();
                })
                .LogTo(_ => { }, [DbLoggerCategory.Database.Command.Name], Microsoft.Extensions.Logging.LogLevel.None);
            },
            ServiceLifetime.Scoped);

        // Repositories
        services.AddScoped<IMeteringPointRepository, MeteringPointRepository>();
        services.AddScoped<IMeteringPointIntegrationRepository, MeteringPointIntegrationRepository>();
        services.AddScoped<IGridAreaRepository, GridAreaRepository>();
        services.AddScoped<IProcessDelegationRepository, ProcessDelegationRepository>();
        services.AddScoped<IImportedTransactionRepository, ImportedTransactionRepository>();

        // Services
        services.AddScoped<ICsvImporter, CsvImporter>();
        services.AddScoped<IRoleFiltrationService, RoleFiltrationService>();
        services.AddScoped<IRelationalModelPrinter, RelationalModelPrinter>();
        services.AddScoped<IElectricalHeatingPeriodizationService, ElectricalHeatingPeriodizationService>();
        services.AddScoped<ISyncJobsRepository, SyncJobRepository>();
        services.AddScoped<IDeltaLakeDataUploadService, DeltaLakeDataUploadService>();

        return services;
    }

    public static IServiceCollection AddElectricityMarketDatabricksModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DatabricksCatalogOptions>()
            .BindConfiguration(DatabricksCatalogOptions.SectionName)
            .ValidateDataAnnotations();

        services
            .AddDatabricksSqlStatementExecution(configuration.GetSection("Databricks"));

        return services;
    }
}
