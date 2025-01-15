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
using Energinet.DataHub.ElectricityMarket.Integration.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Integration;

public sealed class ElectricityMarketViews : IElectricityMarketViews
{
    private readonly ElectricityMarketDatabaseContext _context;

    internal ElectricityMarketViews(ElectricityMarketDatabaseContext context)
    {
        _context = context;
    }

    public IAsyncEnumerable<MeteringPointMasterData> GetMeteringPointMasterDataChangesAsync(
        MeteringPointIdentification meteringPointId,
        Interval period)
    {
        var f = period.Start.ToDateTimeOffset();
        var t = period.End.ToDateTimeOffset();

        var query =
            from change in _context.MeteringPointChanges
            where change.Identification == meteringPointId.Value &&
                  change.ValidFrom <= t &&
                  change.ValidTo > f
            orderby change.ValidFrom
            select new MeteringPointMasterData
            {
                Identification = new MeteringPointIdentification(change.Identification),
                GridAreaCode = new GridAreaCode(change.GridAreaCode),
                GridAccessProvider = ActorNumber.Create(change.GridAccessProvider),
                NeighborGridAreaOwner = null,
                ConnectionState = Enum.Parse<ConnectionState>(change.ConnectionState),
                Type = Enum.Parse<MeteringPointType>(change.Type),
                SubType = Enum.Parse<MeteringPointSubType>(change.SubType),
                Resolution = new Resolution(change.Resolution),
                Unit = Enum.Parse<MeasureUnit>(change.Unit),
                ProductId = Enum.Parse<ProductId>(change.ProductId),
                ParentIdentification = change.ParentIdentification != null
                    ? new MeteringPointIdentification(change.ParentIdentification)
                    : null,
            };

        return query.AsAsyncEnumerable();
    }

    public IAsyncEnumerable<MeteringPointEnergySupplier> GetMeteringPointEnergySuppliersAsync(
        MeteringPointIdentification meteringPointId,
        Interval period)
    {
        var f = period.Start.ToDateTimeOffset();
        var t = period.End.ToDateTimeOffset();

        var query =
            from es in _context.MeteringPointEnergySuppliers
            where es.Identification == meteringPointId.Value &&
                  es.StartDate <= t &&
                  es.EndDate > f
            orderby es.StartDate
            select new MeteringPointEnergySupplier
            {
                Identification = new MeteringPointIdentification(es.Identification),
                EnergySupplier = ActorNumber.Create(es.EnergySupplier),
                StartDate = es.StartDate.ToInstant(),
                EndDate = es.EndDate.ToInstant(),
            };

        return query.AsAsyncEnumerable();
    }
}
