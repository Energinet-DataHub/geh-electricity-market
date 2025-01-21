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
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            ? new MeteringPoint(entity.Id, new MeteringPointIdentification(entity.Identification))
            : null;
    }
}
