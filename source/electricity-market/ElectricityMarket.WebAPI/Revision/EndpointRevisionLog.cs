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

using Energinet.DataHub.RevisionLog.Integration;
using Microsoft.AspNetCore.Http.Extensions;
using NodaTime;

namespace ElectricityMarket.WebAPI.Revision;

public sealed class EndpointRevisionLog
{
    private readonly IRevisionLogClient _revisionLogClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IClock _clock;

    private readonly Guid _systemGuid = Guid.Parse("6A177C9D-4914-4607-BA5A-517C142B7F1F");

    public EndpointRevisionLog(
        IRevisionLogClient revisionLogClient,
        IHttpContextAccessor httpContextAccessor,
        IClock clock)
    {
        _revisionLogClient = revisionLogClient;
        _httpContextAccessor = httpContextAccessor;
        _clock = clock;
    }

    public Task LogAsync(Guid authorizationRequestId, string activity, Type entity, string entityKey)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext required for endpoint revision.");
        }

        var logEntry = new RevisionLogEntry(
            Guid.NewGuid(),
            _systemGuid,
            activity,
            _clock.GetCurrentInstant(),
            httpContext.Request.GetEncodedPathAndQuery(),
            authorizationRequestId.ToString(),
            affectedEntityType: entity.Name,
            affectedEntityKey: entityKey);

        return _revisionLogClient.LogAsync(logEntry);
    }
}
