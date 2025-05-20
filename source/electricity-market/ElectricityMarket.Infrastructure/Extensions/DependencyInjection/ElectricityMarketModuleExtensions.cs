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
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions.DependencyInjection;

public static class ElectricityMarketModuleExtensions
{
    public static IServiceCollection AddElectricityMarketModule(this IServiceCollection services, bool aggressiveEfCoreLogging = false)
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
                .AddInterceptors(new FastInterceptor());

            if (!aggressiveEfCoreLogging)
            {
                o.LogTo(_ => { }, [DbLoggerCategory.Database.Command.Name], LogLevel.None);
            }
            else
            {
                var databaseLogger = p.GetRequiredService<ILogger<DatabaseOptions>>();
#pragma warning disable CA1848, CA2254
                o.LogTo(x => databaseLogger.Log(LogLevel.Warning, x));
#pragma warning restore CA1848, CA2254
            }
        });

        services.AddDbContext<IMarketParticipantDatabaseContext, MarketParticipantDatabaseContext>((p, o) =>
        {
            var databaseOptions = p.GetRequiredService<IOptions<DatabaseOptions>>();
            o.UseSqlServer(databaseOptions.Value.ConnectionString, options =>
                {
                    options.UseNodaTime();
                })
                .LogTo(_ => { }, [DbLoggerCategory.Database.Command.Name], LogLevel.None);
        });

        services.AddDbContextFactory<ElectricityMarketDatabaseContext>(
            (p, o) =>
            {
                var databaseOptions = p.GetRequiredService<IOptions<DatabaseOptions>>();
                o.UseSqlServer(databaseOptions.Value.ConnectionString, options =>
                    {
                        options.UseNodaTime();
                    })
                    .LogTo(_ => { }, [DbLoggerCategory.Database.Command.Name], LogLevel.None)
                    .AddInterceptors(new FastInterceptor());
            },
            ServiceLifetime.Scoped);

        // Repositories
        services.AddScoped<IMeteringPointRepository, MeteringPointRepository>();
        services.AddScoped<IMeteringPointIntegrationRepository, MeteringPointIntegrationRepository>();
        services.AddScoped<IGridAreaRepository, GridAreaRepository>();
        services.AddScoped<IProcessDelegationRepository, ProcessDelegationRepository>();
        services.AddScoped<IImportedTransactionRepository, ImportedTransactionRepository>();
        services.AddScoped<ISyncJobsRepository, SyncJobRepository>();

        // Services
        services.AddScoped<ICsvImporter, CsvImporter>();
        services.AddScoped<IRoleFiltrationService, RoleFiltrationService>();
        services.AddScoped<IRelationalModelPrinter, RelationalModelPrinter>();
        services.AddScoped<IElectricalHeatingPeriodizationService, ElectricalHeatingPeriodizationService>();
        services.AddScoped<ICapacitySettlementService, CapacitySettlementService>();
        services.AddScoped<INetConsumptionService, NetConsumptionService>();
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

    private sealed class FastInterceptor : DbCommandInterceptor
    {
        private const string Marker = "-- FAST1";
        private const string FastHint = " OPTION (FAST 1)";

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            EnableTurboMode(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            EnableTurboMode(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private static void EnableTurboMode(DbCommand command)
        {
            if (command.CommandText.Contains(Marker, StringComparison.InvariantCulture) && !command.CommandText.Contains(FastHint, StringComparison.InvariantCulture))
                command.CommandText += FastHint;
        }
    }
}
