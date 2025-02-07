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

using System.ComponentModel.DataAnnotations;
using ElectricityMarket.Application.Commands.ProcessDelegations;
using ElectricityMarket.Application.Models;
using ElectricityMarket.Domain.Models.Actor;
using ElectricityMarket.Domain.Models.GridAreas;
using ElectricityMarket.Domain.Repositories;
using MediatR;

namespace ElectricityMarket.Application.Handlers;

public sealed class GetProcessDelegationHandler : IRequestHandler<GetProcessDelegationCommand, ProcessDelegationDto>
{
    private readonly IActorRepository _actorRepository;
    private readonly IGridAreaRepository _gridAreaRepository;
    private readonly IProcessDelegationRepository _processDelegationRepository;

    public GetProcessDelegationHandler(
        IActorRepository actorRepository,
        IGridAreaRepository gridAreaRepository,
        IProcessDelegationRepository processDelegationRepository)
    {
        _actorRepository = actorRepository;
        _gridAreaRepository = gridAreaRepository;
        _processDelegationRepository = processDelegationRepository;
    }

    public async Task<ProcessDelegationDto> Handle(GetProcessDelegationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actors = await _actorRepository.GetActorsByNumberAsync(ActorNumber.Create(request.ProcessDelegationRequest.ActorNumber)).ConfigureAwait(false);
        if (!actors.Any())
            throw new ValidationException($"No actors with number: {request.ProcessDelegationRequest.ActorNumber} found");

        var delegatedByActor = actors.SingleOrDefault(x => x.MarketRole.Function == request.ProcessDelegationRequest.ActorRole);
        if (delegatedByActor == null)
            throw new ValidationException($"Market role: {request.ProcessDelegationRequest.ActorRole} was not found for actor: {request.ProcessDelegationRequest.ActorNumber}");

        var gridArea = await _gridAreaRepository.GetGridAreaAsync(new GridAreaCode(request.ProcessDelegationRequest.GridAreaCode)).ConfigureAwait(false);

        if (gridArea == null)
            throw new ValidationException($"The grid area with code: {request.ProcessDelegationRequest.GridAreaCode} was not found");

        var processDelegation = await _processDelegationRepository.GetProcessDelegationAsync(delegatedByActor.Id, request.ProcessDelegationRequest.ProcessType).ConfigureAwait(false);

        if (processDelegation == null)
            throw new ValidationException($"No process delegations for actor: {delegatedByActor.Id} were found");

        var delegation = processDelegation.DelegatedPeriods.SingleOrDefault(x => x.GridAreaId == gridArea.Id);
        if (delegation == null)
            throw new ValidationException($"No delegated periods for grid area code: {request.ProcessDelegationRequest.GridAreaCode} delegated by actor Id: {delegatedByActor.Id} were found");

        var delegatedToActor = await _actorRepository.GetAsync(delegation.DelegatedToActorId).ConfigureAwait(false);
        if (delegatedToActor == null)
            throw new ValidationException($"The delegated to actor with Id: {delegation.DelegatedToActorId} was not found");

        return new ProcessDelegationDto(delegatedToActor.ActorNumber.Value, delegatedToActor.MarketRole.Function);
    }
}
