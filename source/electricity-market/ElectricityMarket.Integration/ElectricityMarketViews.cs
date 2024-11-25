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

using System.Data;
using Energinet.DataHub.ElectricityMarket.Integration.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Integration;

public sealed class ElectricityMarketViews : IElectricityMarketViews
{
    private readonly ElectricityMarketDatabaseContext _context;

    internal ElectricityMarketViews(ElectricityMarketDatabaseContext context)
    {
        _context = context;
    }

    public IAsyncEnumerable<MeteringPointChange> GetMeteringPointChangesAsync(MeteringPointIdentification identification)
    {
        return _context.MeteringPointChanges.Where(x => x.Identification == identification.Value).Select(x => new MeteringPointChange
        {
            Identification = new MeteringPointIdentification(x.Identification),
            ValidFrom = x.ValidFrom.ToInstant(),
            ValidTo = x.ValidTo.ToInstant(),
            GridAreaCode = new GridAreaCode(x.GridAreaCode),
            GridAccessProvider = ActorNumber.Create(x.GridAccessProvider),
            ConnectionState = (ConnectionState)x.ConnectionState,
            SubType = (SubType)x.SubType,
            Resolution = new Resolution(x.Resolution),
            Unit = (MeasureUnit)x.Unit,
            ProductCode = (ProductCode)x.ProductId,
        }).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<MeteringPointEnergySupplier> GetMeteringPointEnergySuppliersAsync(MeteringPointIdentification identification)
    {
        return _context.MeteringPointEnergySuppliers.Where(x => x.Identification == identification.Value).Select(x => new MeteringPointEnergySupplier
        {
            Identification = new MeteringPointIdentification(x.Identification),
            EnergySupplier = ActorNumber.Create(x.EnergySupplier),
            StartDate = x.StartDate.ToInstant(),
            EndDate = x.EndDate.ToInstant(),
        }).AsAsyncEnumerable();
    }
}
