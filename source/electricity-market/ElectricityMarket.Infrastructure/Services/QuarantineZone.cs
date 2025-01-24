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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class QuarantineZone : IQuarantineZone
{
    private readonly IElectricityMarketDatabaseContext _context;

    public QuarantineZone(IElectricityMarketDatabaseContext context)
    {
        _context = context;
    }

    public async Task<bool> IsQuarantinedAsync(MeteringPointTransaction meteringPointTransaction)
    {
        ArgumentNullException.ThrowIfNull(meteringPointTransaction);

        return await _context.QuarantinedMeteringPointEntities.AnyAsync(x => x.Identification == meteringPointTransaction.Identification).ConfigureAwait(false);
    }

    public async Task QuarantineAsync(MeteringPointTransaction meteringPointTransaction, string message)
    {
        ArgumentNullException.ThrowIfNull(meteringPointTransaction);

        var quarantinedMeteringPointEntity = await _context.QuarantinedMeteringPointEntities.FirstOrDefaultAsync(x => x.Identification == meteringPointTransaction.Identification).ConfigureAwait(false);

        if (quarantinedMeteringPointEntity is null)
        {
            quarantinedMeteringPointEntity = new QuarantinedMeteringPointEntity
            {
                Identification = meteringPointTransaction.Identification,
            };

            _context.QuarantinedMeteringPointEntities.Add(quarantinedMeteringPointEntity);

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        _context.QuarantinedMeteringPointTransactionEntities.Add(new QuarantinedMeteringPointTransactionEntity
        {
            BusinessTransactionDosId = meteringPointTransaction.BusinessTransactionDosId,
            MeteringPointStateId = meteringPointTransaction.MeteringPointStateId,
            QuarantinedMeteringPointId = quarantinedMeteringPointEntity.Id,
            Message = message,
        });
    }
}
