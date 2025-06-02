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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class MeteringPointDelegationRepository : IMeteringPointDelegationRepository
{
    private readonly ElectricityMarketDatabaseContext _electricityMarketDatabaseContext;

    public MeteringPointDelegationRepository(ElectricityMarketDatabaseContext electricityMarketDatabaseContext)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
    }

    public async Task<string?> GetMeteringPointDelegatedToActorNumberAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var allowedDelegatedProcesses = new[] { DelegatedProcess.RequestMeteringPointData, DelegatedProcess.SendMeteringPointData, DelegatedProcess.ReceiveMeteringPointData };

        var query = await (from meteringPoint in _electricityMarketDatabaseContext.MeteringPoints
                           where meteringPoint.Identification == identification.Value
                           join meteringPointPeriod in _electricityMarketDatabaseContext.MeteringPointPeriods on meteringPoint.Id equals meteringPointPeriod.MeteringPointId
                           where meteringPointPeriod.ValidFrom <= DateTimeOffset.UtcNow && meteringPointPeriod.ValidTo > DateTimeOffset.UtcNow
                           join gridArea in _electricityMarketDatabaseContext.GridAreas on meteringPointPeriod.GridAreaCode equals gridArea.Code
                           join marketRoleGridArea in _electricityMarketDatabaseContext.MarketRoleGridAreas on gridArea.Id equals marketRoleGridArea.GridAreaId
                           join marketRole in _electricityMarketDatabaseContext.MarketRoles on marketRoleGridArea.MarketRoleId equals marketRole.Id
                           where marketRole.Function == EicFunction.GridAccessProvider
                           join actor in _electricityMarketDatabaseContext.Actors on marketRole.ActorId equals actor.Id
                           join delegation in _electricityMarketDatabaseContext.DelegationPeriods on gridArea.Id equals delegation.GridAreaId
                           where delegation.StartsAt <= DateTimeOffset.UtcNow && (delegation.StopsAt == null || delegation.StopsAt > DateTimeOffset.UtcNow)
                           join processDelegation in _electricityMarketDatabaseContext.ProcessDelegations on delegation.ProcessDelegationId equals processDelegation.Id
                           where allowedDelegatedProcesses.Contains(processDelegation.DelegatedProcess)
                           select new { DelegatedToActorNumber = actor.ActorNumber })
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

        return query?.DelegatedToActorNumber;
    }
}
