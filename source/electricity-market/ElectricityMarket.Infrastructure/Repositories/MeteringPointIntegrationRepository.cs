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

        var query =
            from mp in _electricityMarketDatabaseContext.MeteringPoints
            join mpp in _electricityMarketDatabaseContext.MeteringPointPeriods on mp.Id equals mpp.MeteringPointId
            where mp.Identification == meteringPointIdentification &&
                  mpp.ValidFrom <= endDate &&
                  mpp.ValidTo > startDate &&
                  mpp.RetiredById == null
            join gridArea in gridAreaOwnerQuery on mpp.GridAreaCode equals gridArea.Code
            join exchangeFromGridArea in gridAreaOwnerQuery on mpp.ExchangeFromGridArea equals exchangeFromGridArea.Code into exchangeFrom
            from exchangeFromGridArea in exchangeFrom.DefaultIfEmpty()
            join exchangeToGridArea in gridAreaOwnerQuery on mpp.ExchangeToGridArea equals exchangeToGridArea.Code into exchangeTo
            from exchangeToGridArea in exchangeTo.DefaultIfEmpty()
            orderby mpp.ValidFrom
            select new MeteringPointMasterData
            {
                Identification = new MeteringPointIdentification(mp.Identification),
                ValidFrom = mpp.ValidFrom.ToInstant(),
                ValidTo = mpp.ValidTo.ToInstant(),
                GridAreaCode = new GridAreaCode(mpp.GridAreaCode),
                GridAccessProvider = string.IsNullOrWhiteSpace(mpp.OwnedBy) ? gridArea.ActorNumber : mpp.OwnedBy!,

                // This ugliness is needed for EF Core to translate the left join into a working query.
                NeighborGridAreaOwners = exchangeFromGridArea.ActorNumber != null && exchangeToGridArea.ActorNumber != null
                    ? new List<string> { exchangeFromGridArea.ActorNumber, exchangeToGridArea.ActorNumber }
                    : exchangeFromGridArea.ActorNumber != null && exchangeToGridArea.ActorNumber == null
                        ? new List<string> { exchangeFromGridArea.ActorNumber }
                        : exchangeFromGridArea.ActorNumber == null && exchangeToGridArea.ActorNumber != null
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
                EnergySuppliers = mp.CommercialRelations
                    .Where(cr => cr.StartDate <= endDate && cr.EndDate > startDate && cr.StartDate < cr.EndDate)
                    .Select(cr => new MeteringPointEnergySupplier
                    {
                        Identification = new MeteringPointIdentification(mp.Identification),
                        EnergySupplier = cr.EnergySupplier,
                        StartDate = cr.StartDate.ToInstant(),
                        EndDate = cr.EndDate.ToInstant(),
                    })
                    .ToArray(),
            };

        return query.AsAsyncEnumerable();
    }
}
