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
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Common;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class ProcessDelegationRepository : IProcessDelegationRepository
{
    private readonly IMarketParticipantDatabaseContext _context;

    public ProcessDelegationRepository(IMarketParticipantDatabaseContext context)
    {
        _context = context;
    }

    public async Task<ProcessDelegation?> GetProcessDelegationAsync(ActorId actorId, DelegatedProcess delegatedProcess)
    {
        ArgumentNullException.ThrowIfNull(actorId, nameof(actorId));

        var processDelegation = await _context.ProcessDelegations
            .SingleOrDefaultAsync(x => x.DelegatedByActorId == actorId.Value && x.DelegatedProcess == delegatedProcess)
            .ConfigureAwait(false);

        return processDelegation is null
            ? null
            : ProcessDelegationMapper.MapFromEntity(processDelegation);
    }
}
