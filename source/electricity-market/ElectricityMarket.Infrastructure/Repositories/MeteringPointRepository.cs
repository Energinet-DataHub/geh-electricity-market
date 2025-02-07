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
using ElectricityMarket.Domain.Models;
using ElectricityMarket.Domain.Models.Actors;
using ElectricityMarket.Domain.Models.GridAreas;
using ElectricityMarket.Domain.Models.MasterData;
using ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class MeteringPointRepository : IMeteringPointRepository
{
    private readonly IElectricityMarketDatabaseContext _context;

    public MeteringPointRepository(IElectricityMarketDatabaseContext context)
    {
        _context = context;
    }

    public async Task<MeteringPoint?> GetAsync(MeteringPointIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(identification);

        var entity = await _context.MeteringPoints
            .FirstOrDefaultAsync(x => x.Identification == identification.Value)
            .ConfigureAwait(false);

        return entity is not null
            ? MeteringPointMapper.MapFromEntity(entity)
            : null;
    }

    public IAsyncEnumerable<MeteringPointMasterData> GetMeteringPointMasterDataChangesAsync(string meteringPointIdentification, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var query =
            from mp in _context.MeteringPoints
            join mpp in _context.MeteringPointPeriods on mp.Id equals mpp.MeteringPointId
            where mp.Identification == meteringPointIdentification &&
                  mpp.ValidFrom <= endDate &&
                  mpp.ValidTo > startDate
            orderby mpp.ValidFrom
            select new MeteringPointMasterData
            {
                Identification = new MeteringPointIdentification(mp.Identification),
                ValidFrom = mpp.ValidFrom.ToInstant(),
                ValidTo = mpp.ValidTo.ToInstant(),
                GridAreaCode = new GridAreaCode(mpp.GridAreaCode),
                GridAccessProvider = ActorNumber.Create(mpp.OwnedBy),
                NeighborGridAreaOwners = Array.Empty<ActorNumber>(),
                ConnectionState = Enum.Parse<ConnectionState>(mpp.ConnectionState),
                Type = Enum.Parse<MeteringPointType>(mpp.Type),
                SubType = Enum.Parse<MeteringPointSubType>(mpp.SubType),
                Resolution = new Resolution(mpp.Resolution),
                Unit = Enum.Parse<MeasureUnit>(mpp.Unit),
                ProductId = Enum.Parse<ProductId>(mpp.ProductId),
                ParentIdentification = mpp.ParentIdentification != null
                    ? new MeteringPointIdentification(mpp.ParentIdentification)
                    : null,
            };

        return query.AsAsyncEnumerable();
    }

    public IAsyncEnumerable<MeteringPointRecipient> GetMeteringPointRecipientsAsync(
        string meteringPointIdentification,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        var query =
            from mp in _context.MeteringPoints
            join cr in _context.CommercialRelations on mp.Id equals cr.MeteringPointId
            where mp.Identification == meteringPointIdentification &&
                  cr.StartDate <= endDate &&
                  cr.EndDate > startDate &&
                  cr.StartDate < cr.EndDate
            select new MeteringPointRecipient
            {
                Identification = new MeteringPointIdentification(mp.Identification),
                ActorNumber = ActorNumber.Create(cr.EnergySupplier),
                StartDate = cr.StartDate.ToInstant(),
                EndDate = cr.EndDate.ToInstant(),
                Function = EicFunction.EnergySupplier // hardcoded for now
            };

        return query.AsAsyncEnumerable();
    }
}
