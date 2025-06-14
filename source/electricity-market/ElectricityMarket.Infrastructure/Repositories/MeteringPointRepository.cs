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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Extensions;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class MeteringPointRepository : IMeteringPointRepository
{
    private readonly MarketParticipantDatabaseContext _marketParticipantDatabaseContext;
    private readonly ElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly IRelationalModelPrinter _relationalModelPrinter;
    private readonly IDbContextFactory<ElectricityMarketDatabaseContext> _contextFactory;

    public MeteringPointRepository(
        MarketParticipantDatabaseContext marketParticipantDatabaseContext,
        ElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        IRelationalModelPrinter relationalModelPrinter,
        IDbContextFactory<ElectricityMarketDatabaseContext> contextFactory)
    {
        _marketParticipantDatabaseContext = marketParticipantDatabaseContext;
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _relationalModelPrinter = relationalModelPrinter;
        _contextFactory = contextFactory;
    }

    public async Task<MeteringPoint?> GetAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = await _electricityMarketDatabaseContext.MeteringPoints
            .HintFewRows()
            .AsSplitQuery()
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

    public async Task<MeteringPoint?> GetMeteringPointForSignatureAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = await _electricityMarketDatabaseContext.MeteringPoints
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        return entity == null ? null : MeteringPointMapper.MapFromEntity(entity);
    }

    public async Task<string> GetMeteringPointDebugViewAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = await _electricityMarketDatabaseContext.MeteringPoints
            .HintFewRows()
            .AsSplitQuery()
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

    public async Task<IEnumerable<MeteringPointIdentification>> GetByGridAreaCodeAsync(string gridAreaCode)
    {
        ArgumentNullException.ThrowIfNull(gridAreaCode);

        var query = from mpp in _electricityMarketDatabaseContext.MeteringPointPeriods
                    join mp in _electricityMarketDatabaseContext.MeteringPoints on mpp.MeteringPointId equals mp.Id
                    where mpp.GridAreaCode == gridAreaCode
                    select mp.Identification;

        return (await query.Distinct().ToListAsync().ConfigureAwait(false))
            .Select(x => new MeteringPointIdentification(x));
    }

    public async Task<IEnumerable<MeteringPoint>?> GetRelatedMeteringPointsAsync(MeteringPointIdentification identification)
    {
        var parent = await _electricityMarketDatabaseContext.MeteringPoints
            .HintFewRows()
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        if (parent == null)
            return null;

        var powerPlantGsrn = parent.MeteringPointPeriods
            .Where(x => x.RetiredBy is null)
            .FirstOrDefault(x => x.ValidFrom <= DateTimeOffset.UtcNow && x.ValidTo >= DateTimeOffset.UtcNow)?.PowerPlantGsrn;

        var allRelated = await _electricityMarketDatabaseContext.MeteringPoints
            .HintFewRows()
            .Where(x => x.MeteringPointPeriods.Any(y => y.ParentIdentification == identification.Value || (powerPlantGsrn != null && powerPlantGsrn == y.PowerPlantGsrn)))
            .ToListAsync()
            .ConfigureAwait(false);

        return allRelated.Select(MeteringPointMapper.MapFromEntity);
    }

    public async IAsyncEnumerable<MeteringPoint> GetMeteringPointsToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 10000)
    {
        var readContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        readContext.Database.SetCommandTimeout(60 * 60);

        await using (readContext.ConfigureAwait(false))
        {
            var entities = readContext.MeteringPoints
            .AsSplitQuery()
            .Where(x => x.Version > lastSyncedVersion)
            .OrderBy(x => x.Version)
            .Take(batchSize)
            .AsAsyncEnumerable();

            await foreach (var entity in entities.ConfigureAwait(false))
            {
                yield return MeteringPointMapper.MapFromEntity(entity);
            }
        }
    }

    public async Task<IEnumerable<MeteringPoint>> GetChildMeteringPointsAsync(long identification)
    {
        var childMeteringPoints = await _electricityMarketDatabaseContext.MeteringPoints
            .AsSplitQuery()
            .Where(x => x.MeteringPointPeriods.Any(y => y.ParentIdentification == identification)).ToListAsync()
            .ConfigureAwait(false);

        return childMeteringPoints.Select(MeteringPointMapper.MapFromEntity);
    }

    public IAsyncEnumerable<MeteringPointHierarchy> GetCapacitySettlementMeteringPointHierarchiesToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 50)
    {
        var capacitySettlementTypeString = MeteringPointType.CapacitySettlement.ToString();
        var existsClause = $"""
                           AND EXISTS (
                            SELECT 1
                            FROM [electricitymarket].[MeteringPointPeriod] [mpp]
                            WHERE [mpp].[ParentIdentification] = [mp].[Identification] AND [mpp].[Type] = '{capacitySettlementTypeString}'
                           )
                           """;

        return GetMeteringPointHierarchiesToSyncAsync(existsClause, lastSyncedVersion, batchSize);
    }

    public IAsyncEnumerable<MeteringPointHierarchy> GetNetConsumptionMeteringPointHierarchiesToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 50)
    {
        var settlementGroup6Code = NetSettlementGroup.Group6.Code;
        var existsClause = $"""
                           AND EXISTS (
                            SELECT 1
                            FROM [electricitymarket].[MeteringPointPeriod] [mpp]
                            WHERE [mpp].[MeteringPointId] = [mp].[Id] AND [mpp].[SettlementGroup] = {settlementGroup6Code}
                           )
                           """;
        return GetMeteringPointHierarchiesToSyncAsync(existsClause, lastSyncedVersion, batchSize);
    }

    public IAsyncEnumerable<MeteringPointHierarchy> GetElectricalHeatingMeteringPointHierarchiesToSyncAsync(
        DateTimeOffset lastSyncedVersion, int batchSize = 50)
    {
        var existsClause = """
                           AND EXISTS (
                            SELECT 1
                            FROM [electricitymarket].[CommercialRelation] [cr] JOIN [electricitymarket].[ElectricalHeatingPeriod] [ehp] ON [ehp].[CommercialRelationId] = [cr].[Id]
                            WHERE [cr].[MeteringPointId] = [mp].[Id]
                           )
                           """;

        return GetMeteringPointHierarchiesToSyncAsync(existsClause, lastSyncedVersion, batchSize);
    }

    public IAsyncEnumerable<MeteringPointHierarchy> GetMeteringPointHierarchiesToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize)
    {
        return GetMeteringPointHierarchiesToSyncAsync(null, lastSyncedVersion, batchSize);
    }

    private async IAsyncEnumerable<MeteringPointHierarchy> GetMeteringPointHierarchiesToSyncAsync(string? existsClause, DateTimeOffset lastSyncedVersion, int batchSize)
    {
        var query = $"""
                    SELECT TOP(@batchSize) Hierarchy.ParentIdentification, (CASE WHEN [Hierarchy].[MaxChildVersion] IS NULL OR [Hierarchy].[ParentVersion] > [Hierarchy].[MaxChildVersion] THEN [Hierarchy].[ParentVersion] ELSE [Hierarchy].[MaxChildVersion] END) as MaxVersion FROM
                    (
                        SELECT [mp].[Identification] as [ParentIdentification], [mp].[Version] as [ParentVersion], (SELECT MAX([Version])
                            FROM [electricitymarket].[MeteringPoint] [child_mp]
                            JOIN [electricitymarket].[MeteringPointPeriod] [child_mpp]
                            ON [child_mp].[Id] = [child_mpp].[MeteringPointId]
                            WHERE [child_mpp].[ParentIdentification] = [mp].[Identification]) as MaxChildVersion
                        FROM [electricitymarket].[MeteringPoint] [mp]
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM [electricitymarket].[MeteringPointPeriod] [mpp]
                            WHERE [mpp].[MeteringPointId] = [mp].[Id] AND [mpp].[ParentIdentification] IS NOT NULL
                        ) {existsClause ?? string.Empty}
                    ) AS Hierarchy
                    WHERE (CASE WHEN [Hierarchy].[MaxChildVersion] IS NULL OR [Hierarchy].[ParentVersion] > [Hierarchy].[MaxChildVersion] THEN [Hierarchy].[ParentVersion] ELSE [Hierarchy].[MaxChildVersion] END) > @latestVersion
                    ORDER BY [MaxVersion] ASC;
                    """;

        var readContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        readContext.Database.SetCommandTimeout(60 * 60);

        await using (readContext.ConfigureAwait(false))
        {
            var batchSizeParam = new SqlParameter("batchSize", batchSize);
            var latestVersionParam = new SqlParameter("latestVersion", lastSyncedVersion);
            var changedItems = readContext.Database.SqlQueryRaw<ChangedHierarchy>(query, batchSizeParam, latestVersionParam).AsNoTracking().ToList();

            if (changedItems.Count == 0)
            {
                yield break;
            }

            var versionMap = changedItems.ToDictionary(x => x.ParentIdentification, y => y.MaxVersion);
            var changedMeteringPointIds = versionMap.Keys.ToArray();
            var changedParents = _electricityMarketDatabaseContext.MeteringPoints.AsNoTracking()
                .Where(x => changedMeteringPointIds.Contains(x.Identification)).AsEnumerable().Select(MeteringPointMapper.MapFromEntity).ToList();

            var childrenIdsQuery = $"""
                                   SELECT [mp].[Identification] as [ParentIdentification], [mpp].[MeteringPointId] as [ChildId]
                                   FROM [electricitymarket].[MeteringPoint] [mp]
                                   JOIN [electricitymarket].[MeteringPointPeriod] [mpp] ON [mp].[Identification] = [mpp].[ParentIdentification]
                                   WHERE [mp].[Identification] IN ({string.Join(',', changedMeteringPointIds)});
                                   """;

            var parentChildMap = _electricityMarketDatabaseContext.Database.SqlQueryRaw<ParentChild>(childrenIdsQuery).AsNoTracking().ToList()
                .GroupBy(x => x.ParentIdentification).ToDictionary(x => x.Key, y => y.Select(z => z.ChildId).Distinct().ToList());

            foreach (var parent in changedParents)
            {
                var children = new List<MeteringPointEntity>();
                if (parentChildMap.TryGetValue(parent.Identification.Value, out var childrenIds))
                {
                    children = await _electricityMarketDatabaseContext.MeteringPoints
                        .AsSplitQuery()
                        .AsNoTracking()
                        .Where(x => childrenIds.Contains(x.Id))
                        .ToListAsync()
                        .ConfigureAwait(false);
                }

                var hierarchyVersion = versionMap[parent.Identification.Value];
                yield return new MeteringPointHierarchy(parent, children.Select(MeteringPointMapper.MapFromEntity), hierarchyVersion);
            }
        }
    }
}
