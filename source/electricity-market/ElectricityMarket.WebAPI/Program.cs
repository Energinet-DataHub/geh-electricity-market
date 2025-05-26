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

using System.Reflection;
using Asp.Versioning;
using ElectricityMarket.WebAPI;
using ElectricityMarket.WebAPI.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.WebApp.Extensions.Builder;
using Energinet.DataHub.Core.App.WebApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Logging.LoggingMiddleware;
using Energinet.DataHub.ElectricityMarket.Application.Security;
using Energinet.DataHub.RevisionLog.Integration.Extensions.DependencyInjection;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using Energinet.DataHub.RevisionLog.Integration.WebApi.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLoggingScope(SubsystemInformation.Name);
builder.Services.AddApplicationInsightsForWebApp(SubsystemInformation.Name);
builder.Services.AddHealthChecksForWebApp();

builder.Services
    .AddControllers();

builder.Services
    .AddNodaTimeForApplication()
    .AddApiVersioningForWebApp(new ApiVersion(1, 0))
    .AddSwaggerForWebApp(Assembly.GetExecutingAssembly(), SubsystemInformation.Name);

builder.Services
    .AddJwtBearerAuthenticationForWebApp(builder.Configuration)
    .AddUserAuthenticationForWebApp<FrontendUser, FrontendUserProvider>()
    .AddPermissionAuthorizationForWebApp()
    .AddElectricityMarketWebApiModule(builder.Configuration)
    .AddRevisionLogIntegrationModule(builder.Configuration)
    .AddRevisionLogIntegrationWebApiModule<DefaultRevisionLogEntryHandler>(SubsystemInformation.Id);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseSwaggerForWebApp();
app.UseHttpsRedirection();

app.UseLoggingScope();
app.UseAuthentication();
app.UseAuthorization();
app.UseRevisionLogIntegrationWebApiModule();
app.UseUserMiddlewareForWebApp<FrontendUser>();
app.MapControllers().RequireAuthorization();

app.MapLiveHealthChecks();
app.MapReadyHealthChecks();
app.MapStatusHealthChecks();

app.Run();
