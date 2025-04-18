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

using System.Net.Http.Headers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.Diagnostics.HealthChecks;

public class DatabricksSqlStatementApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DatabricksSqlStatementOptions _options;

    public DatabricksSqlStatementApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<DatabricksSqlStatementOptions> databricksOptions)
    {
        _httpClientFactory = httpClientFactory;
        _options = databricksOptions.Value;
    }

    /// <summary>
    /// Check health of the Databricks Sql Statement Execution API.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>An async task of <see cref="HealthCheckResult"/></returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = CreateHttpClient();
            var url = $"{_options.WorkspaceUrl}/api/2.0/sql/warehouses/{_options.WarehouseId}";
            var response = await httpClient
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Databricks Sql Statement Execution API is unhealthy", ex);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_options.WorkspaceUrl);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.WorkspaceToken);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.BaseAddress = new Uri(_options.WorkspaceUrl);
        return httpClient;
    }
}
