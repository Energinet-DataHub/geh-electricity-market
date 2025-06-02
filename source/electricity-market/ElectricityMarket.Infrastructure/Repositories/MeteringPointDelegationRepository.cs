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
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Common;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class MeteringPointDelegationRepository : IMeteringPointDelegationRepository
{
    private readonly MarketParticipantDatabaseContext _marketParticipantDatabaseContext;
    private readonly ElectricityMarketDatabaseContext _electricityMarketDatabaseContext;

    public MeteringPointDelegationRepository(
        MarketParticipantDatabaseContext marketParticipantDatabaseContext,
        ElectricityMarketDatabaseContext electricityMarketDatabaseContext)
    {
        _marketParticipantDatabaseContext = marketParticipantDatabaseContext;
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
    }

    public async Task<IEnumerable<MeteringPointDelegation>> GetDelegationsAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = await _electricityMarketDatabaseContext.MeteringPoints
            .HintFewRows()
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        if (entity == null)
            return Enumerable.Empty<MeteringPointDelegation>();

        var allGridAreas = entity.MeteringPointPeriods
          .Select(mpp => mpp.GridAreaCode)
          .ToHashSet();

        var gridAreaOwnerQuery =
            await (from gridArea in _marketParticipantDatabaseContext.GridAreas
                   where allGridAreas.Contains(gridArea.Code)
                   join marketRoleGridArea in _marketParticipantDatabaseContext.MarketRoleGridAreas on gridArea.Id equals marketRoleGridArea.GridAreaId
                   join marketRole in _marketParticipantDatabaseContext.MarketRoles on marketRoleGridArea.MarketRoleId equals marketRole.Id
                   where marketRole.Function == EicFunction.GridAccessProvider
                   join actor in _marketParticipantDatabaseContext.Actors on marketRole.ActorId equals actor.Id
                   select new { actor.ActorNumber, ActorId = actor.Id, GridAreaId = gridArea.Id }).ToListAsync().ConfigureAwait(false);

        var gridAreaIdLookup = gridAreaOwnerQuery
            .Select(k => k.GridAreaId)
            .ToHashSet();

        var allowedDelegatedProcesses = new[] { DelegatedProcess.RequestMeteringPointData, DelegatedProcess.SendMeteringPointData, DelegatedProcess.ReceiveMeteringPointData };

        var delegationsQuery = await (from processDelegations in _marketParticipantDatabaseContext.ProcessDelegations
                                      where allowedDelegatedProcesses.Contains(processDelegations.DelegatedProcess)
                                      join delegation in _marketParticipantDatabaseContext.DelegationPeriods on processDelegations.Id equals delegation.ProcessDelegationId
                                      where gridAreaIdLookup.Contains(delegation.GridAreaId)
                                      join actor in _marketParticipantDatabaseContext.Actors on delegation.DelegatedToActorId equals actor.Id
                                      select new
                                      {
                                          DelegatedToActorNumber = actor.ActorNumber,
                                          delegation.StartsAt,
                                          delegation.StopsAt,
                                      })
            .ToListAsync()
            .ConfigureAwait(false);

        return delegationsQuery
            .Select(x => new MeteringPointDelegation(
                new Interval(x.StartsAt.ToInstant(), x.StopsAt?.ToInstant()),
                x.DelegatedToActorNumber))
            .Where(x => !string.IsNullOrEmpty(x.DelegatedActorNumber) && entity.MeteringPointPeriods.Any(mpp => x.ValidPeriod.Contains(mpp.ValidFrom.ToInstant()) || x.ValidPeriod.Contains(mpp.ValidTo.ToInstant())))
            .ToList();
    }
}
