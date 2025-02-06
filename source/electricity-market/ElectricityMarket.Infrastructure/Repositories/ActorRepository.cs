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
using System.Threading.Tasks;
using ElectricityMarket.Domain.Models.Actor;
using ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class ActorRepository : IActorRepository
{
    private readonly IMarketParticipantDatabaseContext _context;

    public ActorRepository(IMarketParticipantDatabaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Actor>> GetActorsByNumberAsync(ActorNumber actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber, nameof(actorNumber));

        var actors = await _context.Actors
            .Where(x => x.ActorNumber == actorNumber.Value)
            .ToListAsync()
            .ConfigureAwait(false);

        return actors.Select(ActorMapper.MapFromEntity);
    }

    public async Task<Actor?> GetAsync(ActorId actorId)
    {
        var foundActor = await _context
            .Actors
            .FirstOrDefaultAsync(actor => actor.Id == actorId.Value)
            .ConfigureAwait(false);

        return foundActor == null
            ? null
            : ActorMapper.MapFromEntity(foundActor);
    }
}
