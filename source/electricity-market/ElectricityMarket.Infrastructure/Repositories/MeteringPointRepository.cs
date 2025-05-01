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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class MeteringPointRepository : IMeteringPointRepository
{
    private readonly MarketParticipantDatabaseContext _marketParticipantDatabaseContext;
    private readonly ElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly IRelationalModelPrinter _relationalModelPrinter;

    public MeteringPointRepository(
        MarketParticipantDatabaseContext marketParticipantDatabaseContext,
        ElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        IRelationalModelPrinter relationalModelPrinter)
    {
        _marketParticipantDatabaseContext = marketParticipantDatabaseContext;
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _relationalModelPrinter = relationalModelPrinter;
    }

    public async Task<MeteringPoint?> GetAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = await _electricityMarketDatabaseContext.MeteringPoints
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        if (entity == null)
            return null;

        var allGridAreas = entity.MeteringPointPeriods
            .Select(mpp => mpp.GridAreaCode)
            .ToHashSet();

        var gridAreaOwnerQuery =
            from gridArea in _marketParticipantDatabaseContext.GridAreas
            where allGridAreas.Contains(gridArea.Code)
            join marketRoleGridArea in _marketParticipantDatabaseContext.MarketRoleGridAreas on gridArea.Id equals marketRoleGridArea.GridAreaId
            join marketRole in _marketParticipantDatabaseContext.MarketRoles on marketRoleGridArea.MarketRoleId equals marketRole.Id
            where marketRole.Function == EicFunction.GridAccessProvider
            join actor in _marketParticipantDatabaseContext.Actors on marketRole.ActorId equals actor.Id
            select new { gridArea.Code, actor.ActorNumber };

        var gridAreaLookup = await gridAreaOwnerQuery
            .ToDictionaryAsync(k => k.Code, v => v.ActorNumber)
            .ConfigureAwait(false);

        foreach (var mpp in entity.MeteringPointPeriods)
        {
            if (!string.IsNullOrWhiteSpace(mpp.GridAreaCode) && gridAreaLookup.TryGetValue(mpp.GridAreaCode, out var actorNumber))
            {
                mpp.OwnedBy = actorNumber;
            }
        }

        return MeteringPointMapper.MapFromEntity(entity);
    }

    public async Task<string> GetMeteringPointDebugViewAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = await _electricityMarketDatabaseContext.MeteringPoints
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        var quarantined = await _electricityMarketDatabaseContext.QuarantinedMeteringPointEntities
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        return await _relationalModelPrinter.PrintAsync(
            entity != null ? [[entity]] : [],
            quarantined != null ? [[quarantined]] : [],
            CultureInfo.GetCultureInfo("da-DK")).ConfigureAwait(false);
    }

    public async Task<IEnumerable<MeteringPoint>> GetByGridAreaCodeAsync(string gridAreaCode)
    {
        ArgumentNullException.ThrowIfNull(gridAreaCode);

        var entities = await _electricityMarketDatabaseContext.MeteringPoints
            .Where(x => x.MeteringPointPeriods.Any(mpp => mpp.GridAreaCode == gridAreaCode))
            .ToListAsync()
            .ConfigureAwait(false);

        return entities.Select(MeteringPointMapper.MapFromEntity);
    }

    public async Task<IEnumerable<MeteringPoint>?> GetRelatedMeteringPointsAsync(MeteringPointIdentification identification)
    {
        var parent = await _electricityMarketDatabaseContext.MeteringPoints
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        if (parent == null)
            return null;

        var powerPlantGsrn = parent.MeteringPointPeriods
            .Where(x => x.RetiredBy is null)
            .FirstOrDefault(x => x.ValidFrom <= DateTimeOffset.UtcNow && x.ValidTo >= DateTimeOffset.UtcNow)?.PowerPlantGsrn;

        var allRelated = await _electricityMarketDatabaseContext.MeteringPoints
            .Where(x => x.MeteringPointPeriods.Any(y => y.ParentIdentification == identification.Value || (powerPlantGsrn != null && powerPlantGsrn == y.PowerPlantGsrn)))
            .ToListAsync()
            .ConfigureAwait(false);

        return allRelated.Select(MeteringPointMapper.MapFromEntity);
    }

    public async IAsyncEnumerable<MeteringPoint> GetMeteringPointsToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 10000)
    {
        var entities = _electricityMarketDatabaseContext.MeteringPoints
            .Where(x => x.Version >= lastSyncedVersion)
            .OrderBy(x => x.Version)
            .Take(batchSize)
            .AsAsyncEnumerable();

        await foreach (var entity in entities)
        {
            yield return MeteringPointMapper.MapFromEntity(entity);
        }
    }

    public async Task<MeteringPointHierarchy> GetMeteringPointHierarchyAsync(
        MeteringPointIdentification identification,
        CancellationToken cancellationToken = default)
    {
        var parent = await _electricityMarketDatabaseContext.MeteringPoints
            .Where(x => (x.Identification == identification.Value && x.MeteringPointPeriods.All(y => y.ParentIdentification == null)) ||
                        _electricityMarketDatabaseContext.MeteringPoints.First(y => y.Identification == identification.Value)
                            .MeteringPointPeriods.Select(m => m.ParentIdentification).Contains(x.Identification))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (parent is null)
        {
            throw new NotFoundException("Parent metering point not found");
        }

        var childMeteringPoints = await _electricityMarketDatabaseContext.MeteringPoints
            .Where(x => x.MeteringPointPeriods.Any(y => y.ParentIdentification == parent.Identification)).ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new MeteringPointHierarchy(MeteringPointMapper.MapFromEntity(parent), childMeteringPoints.Select(MeteringPointMapper.MapFromEntity));
    }
}
