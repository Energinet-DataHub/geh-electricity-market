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

using System.Text.Json;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.Builder;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Hosts.DataApi.Extensions.DependencyInjection;
using Energinet.DataHub.RevisionLog.Integration.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        // Http => Authorization
        builder.UseFunctionsAuthorization();
    })
    .ConfigureServices((context, services) =>
    {
        // Common
        services.Configure<JsonSerializerOptions>(j => j.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
        services.AddApplicationInsightsForIsolatedWorker("electricity-market");
        services.AddHealthChecksForIsolatedWorker();

        // Shared by modules
        services.AddNodaTimeForApplication();

        // Revision log
        services.AddRevisionLogIntegrationModule(context.Configuration);

        // Modules
        services.AddElectricityMarketDataApiModule(context.Configuration);
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddLoggingConfigurationForIsolatedWorker(hostingContext.Configuration);
    })
    .Build();

host.Run();
