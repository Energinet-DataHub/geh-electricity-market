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

using System.Linq;
using System.Threading.Tasks;
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

    public Task<MeteringPointMasterData?> GetMeteringPointMasterDataAsync(
        MeteringPointIdentification meteringPointId,
        Instant validAt)
    {
        var validDate = validAt.ToDateTimeOffset();

        var query =
            from change in _context.MeteringPointChanges
            where change.Identification == meteringPointId.Value &&
                  change.ValidFrom <= validDate &&
                  change.ValidTo > validDate &&
                  change.GridAccessProviderPeriodFrom <= validDate &&
                  change.GridAccessProviderPeriodTo > validDate
            select new MeteringPointMasterData
            {
                Identification = new MeteringPointIdentification(change.Identification),
                GridAreaCode = new GridAreaCode(change.GridAreaCode),
                GridAccessProvider = ActorNumber.Create(change.GridAccessProvider),
                ConnectionState = (ConnectionState)change.ConnectionState,
                Type = (MeteringPointType)change.Type,
                SubType = (MeteringPointSubType)change.SubType,
                Resolution = new Resolution(change.Resolution),
                Unit = (MeasureUnit)change.Unit,
                ProductId = (ProductId)change.ProductId,
            };

        return query.FirstOrDefaultAsync();
    }

    public Task<MeteringPointEnergySupplier?> GetMeteringPointEnergySupplierAsync(
        MeteringPointIdentification meteringPointId,
        Instant validAt)
    {
        var validDate = validAt.ToDateTimeOffset();

        var query =
            from es in _context.MeteringPointEnergySuppliers
            where es.Identification == meteringPointId.Value &&
                  es.StartDate <= validDate &&
                  es.EndDate > validDate
            select new MeteringPointEnergySupplier
            {
                Identification = new MeteringPointIdentification(es.Identification),
                EnergySupplier = ActorNumber.Create(es.EnergySupplier),
                StartDate = es.StartDate.ToInstant(),
                EndDate = es.EndDate.ToInstant(),
            };

        return query.FirstOrDefaultAsync();
    }
}
