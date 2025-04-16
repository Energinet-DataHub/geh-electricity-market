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
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;
using ConnectionState = Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData.ConnectionState;
using MeteringPointIdentification = Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData.MeteringPointIdentification;
using MeteringPointSubType = Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData.MeteringPointSubType;
using MeteringPointType = Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData.MeteringPointType;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class MeteringPointIntegrationRepository : IMeteringPointIntegrationRepository
{
    private readonly ElectricityMarketDatabaseContext _electricityMarketDatabaseContext;

    public MeteringPointIntegrationRepository(ElectricityMarketDatabaseContext electricityMarketDatabaseContext)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
    }

    public async IAsyncEnumerable<MeteringPointMasterData> GetMeteringPointMasterDataChangesTake2Async(
            string meteringPointIdentification,
            DateTimeOffset startDate,
            DateTimeOffset endDate)
        {
            var meteringPoint = await _electricityMarketDatabaseContext.MeteringPoints
                .FirstOrDefaultAsync(mp => mp.Identification == meteringPointIdentification).ConfigureAwait(false);

            if (meteringPoint == null)
            {
                yield break;
            }

            var gridAreaOwnerQuery =
                from actor in _electricityMarketDatabaseContext.Actors
                where actor.MarketRole.Function == EicFunction.GridAccessProvider
                from ownedGridArea in actor.MarketRole.GridAreas
                join gridArea in _electricityMarketDatabaseContext.GridAreas on ownedGridArea.GridAreaId equals gridArea.Id
                select new { gridArea.Code, actor.ActorNumber };

            var commercialRelations = meteringPoint.CommercialRelations
                .Where(cr => cr.StartDate <= endDate && cr.EndDate > startDate && cr.StartDate < cr.EndDate).ToList();

            var meteringPointPeriods = _electricityMarketDatabaseContext.MeteringPointPeriods
                .Where(mpp => mpp.MeteringPointId == meteringPoint.Id &&
                              mpp.ValidFrom <= endDate &&
                              mpp.ValidTo > startDate &&
                              mpp.RetiredById == null);

            var mppList = await meteringPointPeriods.OrderBy(x => x.ValidFrom).ToListAsync().ConfigureAwait(false);

            foreach (var mpp in mppList)
            {
                var overlappingCRs = commercialRelations
                    .Where(cr => cr.StartDate <= mpp.ValidTo && cr.EndDate > mpp.ValidFrom).ToList();

                var foobarPeriods = GenerateMeteringPointDataPeriodSets(meteringPoint, mpp, overlappingCRs);

                foreach (var period in foobarPeriods)
                {
                    var gridAreaOwner = await gridAreaOwnerQuery
                        .FirstOrDefaultAsync(g => g.Code == mpp.GridAreaCode).ConfigureAwait(false);

                    var exchangeFromOwner = string.IsNullOrWhiteSpace(mpp.ExchangeFromGridArea)
                        ? null
                        : await gridAreaOwnerQuery.FirstOrDefaultAsync(g => g.Code == mpp.ExchangeFromGridArea).ConfigureAwait(false);

                    var exchangeToOwner = string.IsNullOrWhiteSpace(mpp.ExchangeToGridArea)
                        ? null
                        : await gridAreaOwnerQuery.FirstOrDefaultAsync(g => g.Code == mpp.ExchangeToGridArea).ConfigureAwait(false);

                    var neighbors = new List<string>();
                    if (exchangeFromOwner?.ActorNumber != null) neighbors.Add(exchangeFromOwner.ActorNumber);
                    if (exchangeToOwner?.ActorNumber != null) neighbors.Add(exchangeToOwner.ActorNumber);

                    yield return new MeteringPointMasterData
                    {
                        Identification = new MeteringPointIdentification(meteringPoint.Identification),
                        ValidFrom = period.PStart.ToInstant(),
                        ValidTo = period.PEnd.ToInstant(),
                        GridAreaCode = new GridAreaCode(mpp.GridAreaCode),
                        GridAccessProvider = string.IsNullOrWhiteSpace(mpp.OwnedBy)
                            ? gridAreaOwner?.ActorNumber!
                            : mpp.OwnedBy!,

                        NeighborGridAreaOwners = neighbors,
                        ConnectionState = Enum.Parse<ConnectionState>(mpp.ConnectionState),
                        Type = Enum.Parse<MeteringPointType>(mpp.Type),
                        SubType = Enum.Parse<MeteringPointSubType>(mpp.SubType),
                        Resolution = new Resolution(mpp.Resolution),
                        Unit = Enum.Parse<MeasureUnit>(mpp.MeasureUnit),
                        ProductId = Enum.Parse<ProductId>(mpp.Product),
                        ParentIdentification = mpp.ParentIdentification != null
                            ? new MeteringPointIdentification(mpp.ParentIdentification)
                            : null,
                        EnergySupplier = period.CR?.EnergySupplier
                    };
                }
            }
        }

    public IAsyncEnumerable<MeteringPointMasterData> GetMeteringPointMasterDataChangesAsync(
        string meteringPointIdentification,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        var gridAreaOwnerQuery =
            from actor in _electricityMarketDatabaseContext.Actors
            where actor.MarketRole.Function == EicFunction.GridAccessProvider
            from ownedGridArea in actor.MarketRole.GridAreas
            join gridArea in _electricityMarketDatabaseContext.GridAreas on ownedGridArea.GridAreaId equals gridArea.Id
            select new { gridArea.Code, actor.ActorNumber };

        var meteringPointQuery =
            from mp in _electricityMarketDatabaseContext.MeteringPoints
            where mp.Identification == meteringPointIdentification
            select mp;

        var commercialRelationQuery =
            from cr in meteringPointQuery.Single().CommercialRelations
            where cr.StartDate <= endDate
                  && cr.EndDate > startDate
                  && cr.StartDate < cr.EndDate
            select cr;

        var meteringPointPeriodQuery =
            from mpp in _electricityMarketDatabaseContext.MeteringPointPeriods
            where mpp.MeteringPointId == meteringPointQuery.Single().Id
                  && mpp.ValidFrom <= endDate
                  && mpp.ValidTo > startDate
                  && mpp.RetiredById == null
            select mpp;

        var mppCrGroups =
            meteringPointPeriodQuery.ToList() // TODO (MWO): Would be nice to avoid this ToList() call
                .Select(
                    mpp => new
                {
                    MPP = mpp,
                    CRS = commercialRelationQuery.Where(
                        cr => cr.StartDate <= mpp.ValidTo && cr.EndDate > mpp.ValidFrom),
                });

        var mppCrPeriods =
            mppCrGroups.ToList() // TODO (MWO): Would be nice to avoid this ToList() call
                .SelectMany(
                    grp => grp.CRS.SelectMany(
                        cr =>
                    {
                        if (grp.MPP.ValidTo <= cr.StartDate)
                        {
                            return new List<MeteringPointDataPeriod>
                            {
                                new MeteringPointDataPeriod(
                                    grp.MPP.ValidFrom,
                                    grp.MPP.ValidTo,
                                    meteringPointQuery.Single(),
                                    grp.MPP,
                                    null)
                            };
                        }

                        if (cr.EndDate <= grp.MPP.ValidFrom)
                        {
                            return new List<MeteringPointDataPeriod>
                            {
                                new MeteringPointDataPeriod(
                                    grp.MPP.ValidFrom,
                                    grp.MPP.ValidTo,
                                    meteringPointQuery.Single(),
                                    grp.MPP,
                                    null)
                            };
                        }

                        if (grp.MPP.ValidFrom >= cr.StartDate && grp.MPP.ValidTo <= cr.EndDate)
                        {
                            return new List<MeteringPointDataPeriod>
                            {
                                new MeteringPointDataPeriod(
                                    grp.MPP.ValidFrom,
                                    grp.MPP.ValidTo,
                                    meteringPointQuery.Single(),
                                    grp.MPP,
                                    cr)
                            };
                        }

                        if (cr.StartDate >= grp.MPP.ValidFrom && cr.EndDate <= grp.MPP.ValidTo)
                        {
                            return new List<MeteringPointDataPeriod>
                            {
                                new MeteringPointDataPeriod(
                                    grp.MPP.ValidFrom,
                                    cr.StartDate,
                                    meteringPointQuery.Single(),
                                    grp.MPP,
                                    null),
                                new MeteringPointDataPeriod(cr.StartDate, cr.EndDate, meteringPointQuery.Single(), grp.MPP, cr),
                                new MeteringPointDataPeriod(cr.EndDate, grp.MPP.ValidTo, meteringPointQuery.Single(), grp.MPP, null),
                            };
                        }

                        if (cr.StartDate <= grp.MPP.ValidFrom)
                        {
                            return new List<MeteringPointDataPeriod>
                            {
                                new MeteringPointDataPeriod(grp.MPP.ValidFrom, cr.EndDate, meteringPointQuery.Single(), grp.MPP, cr),
                                new MeteringPointDataPeriod(cr.EndDate, grp.MPP.ValidTo, meteringPointQuery.Single(), grp.MPP, null),
                            };
                        }

                        if (cr.StartDate >= grp.MPP.ValidFrom)
                        {
                            return new List<MeteringPointDataPeriod>
                            {
                                new MeteringPointDataPeriod(
                                    grp.MPP.ValidFrom,
                                    cr.StartDate,
                                    meteringPointQuery.Single(),
                                    grp.MPP,
                                    null),
                                new MeteringPointDataPeriod(cr.StartDate, grp.MPP.ValidTo, meteringPointQuery.Single(), grp.MPP, cr),
                            };
                        }

                        throw new InvalidOperationException("Unexpected date range");
                    }));

        var query =
            from l in mppCrPeriods
            let mp = l.MP
            let mpp = l.MPP
            let cr = l.CR
            join gridArea in gridAreaOwnerQuery on mpp.GridAreaCode equals gridArea.Code
            join exchangeFromGridArea in gridAreaOwnerQuery on mpp.ExchangeFromGridArea equals exchangeFromGridArea.Code into exchangeFrom
            from exchangeFromGridArea in exchangeFrom.DefaultIfEmpty()
            join exchangeToGridArea in gridAreaOwnerQuery on mpp.ExchangeToGridArea equals exchangeToGridArea.Code into exchangeTo
            from exchangeToGridArea in exchangeTo.DefaultIfEmpty()
            orderby mpp.ValidFrom
            select new MeteringPointMasterData
            {
                Identification = new MeteringPointIdentification(mp.Identification),
                ValidFrom = l.PStart.ToInstant(),
                ValidTo = l.PEnd.ToInstant(),
                GridAreaCode = new GridAreaCode(mpp.GridAreaCode),
                GridAccessProvider = string.IsNullOrWhiteSpace(mpp.OwnedBy) ? gridArea.ActorNumber : mpp.OwnedBy!,

                // This ugliness is needed for EF Core to translate the left join into a working query.
                NeighborGridAreaOwners =
                    exchangeFromGridArea?.ActorNumber != null && exchangeToGridArea?.ActorNumber != null
                    ? new List<string> { exchangeFromGridArea.ActorNumber, exchangeToGridArea.ActorNumber }
                    : exchangeFromGridArea?.ActorNumber != null && exchangeToGridArea?.ActorNumber == null
                        ? new List<string> { exchangeFromGridArea.ActorNumber }
                        : exchangeFromGridArea?.ActorNumber == null && exchangeToGridArea?.ActorNumber != null
                            ? new List<string> { exchangeToGridArea.ActorNumber }
                            : new List<string>(),

                ConnectionState = Enum.Parse<ConnectionState>(mpp.ConnectionState),
                Type = Enum.Parse<MeteringPointType>(mpp.Type),
                SubType = Enum.Parse<MeteringPointSubType>(mpp.SubType),
                Resolution = new Resolution(mpp.Resolution),
                Unit = Enum.Parse<MeasureUnit>(mpp.MeasureUnit),
                ProductId = Enum.Parse<ProductId>(mpp.Product),
                ParentIdentification = mpp.ParentIdentification != null
                    ? new MeteringPointIdentification(mpp.ParentIdentification!)
                    : null,
                EnergySupplier = cr?.EnergySupplier,
            };

        return query.ToAsyncEnumerable();
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    private static IEnumerable<MeteringPointDataPeriod> GenerateMeteringPointDataPeriodSets(
        MeteringPointEntity mp,
        MeteringPointPeriodEntity mpp,
        List<CommercialRelationEntity> commercialRelations)
    {
        var now = mpp.ValidFrom;

        foreach (var cre in commercialRelations.OrderBy(x => x.StartDate))
        {
            var creStart = Max(cre.StartDate, mpp.ValidFrom);
            var creEnd = Min(cre.EndDate, mpp.ValidTo);
            if (creStart > now)
            {
                yield return new MeteringPointDataPeriod(now, creStart, mp, mpp, null);
            }

            // Avoid zero-length periods
            if (cre.StartDate != creEnd)
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
        MeteringPointEntity MP,
        MeteringPointPeriodEntity MPP,
        CommercialRelationEntity? CR);
}
