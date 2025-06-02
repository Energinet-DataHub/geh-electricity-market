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

    public async Task<bool> IsMeteringPointDelegatedAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var allowedDelegatedProcesses = new[] { DelegatedProcess.RequestMeteringPointData, DelegatedProcess.SendMeteringPointData, DelegatedProcess.ReceiveMeteringPointData };

        return await _electricityMarketDatabaseContext.MeteringPoints
            .Where(meteringPoint => meteringPoint.Identification == identification.Value)
            .Join(
                _electricityMarketDatabaseContext.MeteringPointPeriods,
                meteringPoint => meteringPoint.Id,
                meteringPointPeriod => meteringPointPeriod.MeteringPointId,
                (meteringPoint, meteringPointPeriod) => new { meteringPoint, meteringPointPeriod })
            .Where(x => x.meteringPointPeriod.ValidFrom <= DateTimeOffset.UtcNow && x.meteringPointPeriod.ValidTo > DateTimeOffset.UtcNow)
            .Join(
                _electricityMarketDatabaseContext.GridAreas,
                x => x.meteringPointPeriod.GridAreaCode,
                gridArea => gridArea.Code,
                (x, gridArea) => new { x.meteringPoint, x.meteringPointPeriod, gridArea })
            .Join(
                _electricityMarketDatabaseContext.MarketRoleGridAreas,
                x => x.gridArea.Id,
                marketRoleGridArea => marketRoleGridArea.GridAreaId,
                (x, marketRoleGridArea) => new { x.meteringPoint, x.meteringPointPeriod, x.gridArea, marketRoleGridArea })
            .Join(
                _electricityMarketDatabaseContext.MarketRoles,
                x => x.marketRoleGridArea.MarketRoleId,
                marketRole => marketRole.Id,
                (x, marketRole) => new { x.meteringPoint, x.meteringPointPeriod, x.gridArea, x.marketRoleGridArea, marketRole })
            .Where(x => x.marketRole.Function == EicFunction.GridAccessProvider)
            .Join(
                _electricityMarketDatabaseContext.Actors,
                x => x.marketRole.ActorId,
                actor => actor.Id,
                (x, actor) => new { x.meteringPoint, x.meteringPointPeriod, x.gridArea, x.marketRoleGridArea, x.marketRole, actor })
            .Join(
                _electricityMarketDatabaseContext.DelegationPeriods,
                x => x.gridArea.Id,
                delegation => delegation.GridAreaId,
                (x, delegation) => new { x.meteringPoint, x.meteringPointPeriod, x.gridArea, x.marketRoleGridArea, x.marketRole, x.actor, delegation })
            .Where(x => x.delegation.StartsAt <= DateTimeOffset.UtcNow && (x.delegation.StopsAt == null || x.delegation.StopsAt > DateTimeOffset.UtcNow))
            .Join(
                _electricityMarketDatabaseContext.ProcessDelegations,
                x => x.delegation.ProcessDelegationId,
                processDelegation => processDelegation.Id,
                (x, processDelegation) => processDelegation.DelegatedProcess)
            .AnyAsync(delegatedProcess => allowedDelegatedProcesses.Contains(delegatedProcess))
            .ConfigureAwait(false);
    }
}
