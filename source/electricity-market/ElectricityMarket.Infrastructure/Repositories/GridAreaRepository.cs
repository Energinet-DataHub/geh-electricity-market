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

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Integration.Models.GridAreas;
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

    public async Task<GridAreaOwnerDto?> GetGridAreaOwnerAsync(string gridAreaCode)
    {
        var gridAreaOwner = await (
                from actor in _context.Actors
                join gridArea in _context.GridAreas on actor.MarketRole.GridAreas.Single().GridAreaId equals gridArea.Id
                where gridArea.Code == gridAreaCode && actor.MarketRole.Function == EicFunction.GridAccessProvider
                select new GridAreaOwnerDto(actor.ActorNumber))
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);
        return gridAreaOwner;
    }
}
