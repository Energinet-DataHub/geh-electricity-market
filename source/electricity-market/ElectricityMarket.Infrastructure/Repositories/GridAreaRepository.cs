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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ElectricityMarket.Domain.Models;
using ElectricityMarket.Domain.Models.GridArea;
using ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class GridAreaRepository : IGridAreaRepository
{
    private readonly IMarketParticipantDatabaseContext _context;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public GridAreaRepository(IMarketParticipantDatabaseContext context)
    {
        _context = context;
        _jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public async IAsyncEnumerable<GridAreaOwnershipAssignedEvent> GetGridAreaOwnershipAssignedEventsAsync()
    {
        var events = await _context.DomainEvents
            .Where(x => x.EventTypeName == "GridAreaOwnershipAssigned" && x.IsSent)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var domainEvent in events)
        {
            var gridOwnershipEvent = JsonSerializer.Deserialize<GridAreaOwnershipAssignedEvent>(
                domainEvent.Event,
                _jsonSerializerOptions);

            if (gridOwnershipEvent == null)
            {
                throw new InvalidOperationException($"Could not deserialize event: {domainEvent.EntityId}");
            }

            yield return gridOwnershipEvent;
        }
    }

    public async Task<GridArea?> GetGridAreaAsync(GridAreaCode code)
    {
        ArgumentNullException.ThrowIfNull(code, nameof(code));

        var gridArea = await _context.GridAreas
            .SingleOrDefaultAsync(x => x.Code == code.Value)
            .ConfigureAwait(false);

        return gridArea is null ? null : GridAreaMapper.MapFromEntity(gridArea);
    }
}
