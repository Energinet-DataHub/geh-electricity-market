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

using ElectricityMarket.WebAPI.Revision;
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Microsoft.AspNetCore.Http.Extensions;

namespace ElectricityMarket.WebAPI.Security;

// TODO: Move to package.
public sealed class EndpointAuthorizationLogger : IEndpointAuthorizationLogger
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Func<EndpointAuthorizationLog, Task> _logCallback;

    public EndpointAuthorizationLogger(
        IHttpContextAccessor httpContextAccessor,
        Func<EndpointAuthorizationLog, Task> logCallback)
    {
        _httpContextAccessor = httpContextAccessor;
        _logCallback = logCallback;
    }

    public Task LogAsync(AccessValidationRequest accessValidationRequest, Signature? signature)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext required for endpoint authorization.");
        }

        return _logCallback(new EndpointAuthorizationLog(
            Guid.NewGuid(), // TODO: Will get from package.
            httpContext.Request.GetEncodedPathAndQuery(),
            accessValidationRequest.GetActivity(),
            accessValidationRequest.GetEntityType(),
            accessValidationRequest.GetEntityKey()));
    }
}
