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
using Energinet.DataHub.ElectricityMarket.Application.Mappers;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.ElectricityMarket.Integration.Models.ProcessDelegation;
using Microsoft.EntityFrameworkCore;
using DelegatedProcess = Energinet.DataHub.ElectricityMarket.Domain.Models.Common.DelegatedProcess;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class ProcessDelegationRepository : IGetProcessDelegation
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

    public async Task<ProcessDelegationDto?> GetProcessDelegationAsync(ProcessDelegationRequestDto processDelegationRequest)
    {
        var delegation = await (
            from actor in _context.Actors
            where actor.ActorNumber == processDelegationRequest.ActorNumber && EicFunctionMapper.Map(actor.MarketRole.Function) == processDelegationRequest.ActorRole
            join processDelegation in _context.ProcessDelegations on actor.ActorId equals processDelegation.DelegatedByActorId
            join gridArea in _context.GridAreas on processDelegation.Delegations.SingleOrDefault().GridAreaId equals gridArea.Id
            where gridArea.Code == processDelegationRequest.GridAreaCode && DelegationProcessMapper.Map(processDelegation.DelegatedProcess) == processDelegationRequest.ProcessType
            select processDelegation.Delegations.SingleOrDefault(x => x.GridAreaId == gridArea.Id))
        .SingleOrDefaultAsync().ConfigureAwait(false);

        return null;
    }
}
