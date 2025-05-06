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
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class MeteringPointIntegrationRepository : IMeteringPointIntegrationRepository
{
    private readonly ElectricityMarketDatabaseContext _electricityMarketDatabaseContext;

    public MeteringPointIntegrationRepository(ElectricityMarketDatabaseContext electricityMarketDatabaseContext)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
    }

    public async IAsyncEnumerable<MeteringPointMasterData> GetMeteringPointMasterDataChangesAsync(
        string meteringPointIdentification,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        if (!long.TryParse(meteringPointIdentification, CultureInfo.InvariantCulture, out var meteringPointIdent))
        {
            throw new ArgumentException("Invalid metering point identification.", nameof(meteringPointIdentification));
        }

        // TODO: Perf warning. This will fetch all data for metering point, including contacts etc. Then the data is fetched again further down.
        var meteringPoint = await _electricityMarketDatabaseContext.MeteringPoints
            .AsSplitQuery()
            .FirstOrDefaultAsync(mp => mp.Identification == meteringPointIdent)
            .ConfigureAwait(false);

        if (meteringPoint == null)
        {
            yield break;
        }

        var gridAreaOwnerDictionary = await (
            from actor in _electricityMarketDatabaseContext.Actors
            where actor.MarketRole.Function == EicFunction.GridAccessProvider
            from ownedGridArea in actor.MarketRole.GridAreas
            join gridArea in _electricityMarketDatabaseContext.GridAreas on ownedGridArea.GridAreaId equals gridArea.Id
            select new { gridArea.Code, actor.ActorNumber })
        .ToDictionaryAsync(g => g.Code, g => g.ActorNumber)
        .ConfigureAwait(false);

        var commercialRelations = meteringPoint.CommercialRelations
            .Where(cr => cr.StartDate <= endDate && cr.EndDate > startDate && cr.StartDate < cr.EndDate)
            .ToList();

        var mppList = await _electricityMarketDatabaseContext.MeteringPointPeriods
            .Where(mpp =>
                mpp.MeteringPointId == meteringPoint.Id &&
                mpp.ValidFrom < endDate &&
                mpp.ValidTo >= startDate &&
                mpp.RetiredById == null)
            .OrderBy(x => x.ValidFrom)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var mpp in mppList)
        {
            var overlappingCRs = commercialRelations
                .Where(cr => cr.StartDate <= mpp.ValidTo && cr.EndDate > mpp.ValidFrom)
                .ToList();

            var periods = GenerateMeteringPointDataPeriodSets(meteringPoint, mpp, overlappingCRs);

            foreach (var period in periods)
            {
                if (period.PEnd <= startDate)
                    continue;

                if (period.PStart >= endDate)
                    continue;

                gridAreaOwnerDictionary.TryGetValue(mpp.GridAreaCode, out var gridAccessProviderActorNumber);

                var neighbors = new List<string>();
                if (!string.IsNullOrWhiteSpace(mpp.ExchangeFromGridArea) &&
                    gridAreaOwnerDictionary.TryGetValue(mpp.ExchangeFromGridArea, out var fromOwner))
                {
                    neighbors.Add(fromOwner);
                }

                if (!string.IsNullOrWhiteSpace(mpp.ExchangeToGridArea) &&
                    gridAreaOwnerDictionary.TryGetValue(mpp.ExchangeToGridArea, out var toOwner))
                {
                    neighbors.Add(toOwner);
                }

                yield return new MeteringPointMasterData
                {
                    Identification = new MeteringPointIdentification(meteringPointIdentification),
                    ValidFrom = period.PStart.ToInstant(),
                    ValidTo = period.PEnd.ToInstant(),
                    GridAreaCode = new GridAreaCode(mpp.GridAreaCode),
                    GridAccessProvider = string.IsNullOrWhiteSpace(mpp.OwnedBy)
                        ? gridAccessProviderActorNumber!
                        : mpp.OwnedBy!,

                    NeighborGridAreaOwners = neighbors,
                    ConnectionState = Enum.Parse<ConnectionState>(mpp.ConnectionState),
                    Type = Enum.Parse<MeteringPointType>(mpp.Type),
                    SubType = Enum.Parse<MeteringPointSubType>(mpp.SubType),
                    Resolution = new Resolution(mpp.Resolution),
                    Unit = Enum.Parse<MeasureUnit>(mpp.MeasureUnit),
                    ProductId = Enum.Parse<ProductId>(mpp.Product),
                    ParentIdentification = mpp.ParentIdentification != null
                        ? new MeteringPointIdentification(mpp.ParentIdentification.Value.ToString(CultureInfo.InvariantCulture))
                        : null,
                    EnergySupplier = period.Cr?.EnergySupplier
                };
            }
        }
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    private static IEnumerable<MeteringPointDataPeriod> GenerateMeteringPointDataPeriodSets(
        MeteringPointEntity mp,
        MeteringPointPeriodEntity mpp,
        List<CommercialRelationEntity> commercialRelations)
    {
        var now = mpp.ValidFrom;

        foreach (var cre in commercialRelations)
        {
            var creStart = Max(cre.StartDate, mpp.ValidFrom);
            var creEnd = Min(cre.EndDate, mpp.ValidTo);
            if (creStart > now)
            {
                yield return new MeteringPointDataPeriod(now, creStart, mp, mpp, null);
            }

            // Avoid zero-length periods
            if (creStart != creEnd)
            {
                yield return new MeteringPointDataPeriod(creStart, creEnd, mp, mpp, cre);
            }

            now = creEnd;
        }

        if (now < mpp.ValidTo)
            yield return new MeteringPointDataPeriod(now, mpp.ValidTo, mp, mpp, null);
    }

    private sealed record MeteringPointDataPeriod(
        DateTimeOffset PStart,
        DateTimeOffset PEnd,
        MeteringPointEntity Mp,
        MeteringPointPeriodEntity Mpp,
        CommercialRelationEntity? Cr);
}
