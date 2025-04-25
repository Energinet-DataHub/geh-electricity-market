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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Integration.Models.ProcessDelegation;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class ProcessDelegationRepository : IProcessDelegationRepository
{
    private readonly IMarketParticipantDatabaseContext _context;

    public ProcessDelegationRepository(IMarketParticipantDatabaseContext context)
    {
        _context = context;
    }

    public async Task<ProcessDelegationDto?> GetProcessDelegationAsync(ProcessDelegationRequestDto processDelegationRequest)
    {
        ArgumentNullException.ThrowIfNull(processDelegationRequest, nameof(processDelegationRequest));

        var mappedEicFunction = EicFunctionMapper.Map(processDelegationRequest.ActorRole);
        var mappedProcessType = DelegationProcessMapper.Map(processDelegationRequest.ProcessType);
        var requestDate = DateTimeOffset.UtcNow;
        var delegation = await (
                from actor in _context.Actors
                join processDelegation in _context.ProcessDelegations on actor.Id equals processDelegation.DelegatedByActorId
                join delegationPeriods in _context.DelegationPeriods on processDelegation.Id equals delegationPeriods.ProcessDelegationId
                join gridArea in _context.GridAreas on delegationPeriods.GridAreaId equals gridArea.Id
                join delegatedToActor in _context.Actors on delegationPeriods.DelegatedToActorId equals delegatedToActor.Id
                where gridArea.Code == processDelegationRequest.GridAreaCode
                      && processDelegation.DelegatedProcess == mappedProcessType
                      && actor.ActorNumber == processDelegationRequest.ActorNumber
                      && actor.MarketRole.Function == mappedEicFunction
                      && delegationPeriods.StartsAt <= requestDate && (delegationPeriods.StopsAt == null || delegationPeriods.StopsAt > requestDate)
                select new ProcessDelegationDto(delegatedToActor.ActorNumber, EicFunctionMapper.Map(delegatedToActor.MarketRole.Function)))
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);

        return delegation;
    }
}
