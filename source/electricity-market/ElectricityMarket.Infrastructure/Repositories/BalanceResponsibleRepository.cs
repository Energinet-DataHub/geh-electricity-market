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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories
{
    public class BalanceResponsibleRepository(IDbContextFactory<MarketParticipantDatabaseContext> marketParticipantFactory) : IBalanceResponsibleRepository
    {
        private readonly IDbContextFactory<MarketParticipantDatabaseContext> _marketParticipantFactory = marketParticipantFactory;

        public async Task<string?> GetBalanceResponsiblePartyIdByEnergySupplierAsync(string energySupplierId)
        {
            using var marketParticipantDbContext =
                await _marketParticipantFactory.CreateDbContextAsync().ConfigureAwait(false);

            var now = DateTimeOffset.UtcNow;

            if (!Guid.TryParse(energySupplierId, out var supplierGuid))
                throw new ArgumentException("Invalid GUID format for EnergySupplierId", nameof(energySupplierId));

            var relation = await marketParticipantDbContext.BalanceResponsibleRelations
                .AsNoTracking()
                .Where(r =>
                    r.EnergySupplierId == supplierGuid &&
                    r.ValidFrom <= now &&
                    (r.ValidTo == null || now < r.ValidTo))
                .OrderBy(r => r.ValidFrom)
                .LastOrDefaultAsync().ConfigureAwait(false);

            return relation?.BalanceResponsiblePartyId.ToString();
        }
    }
}
