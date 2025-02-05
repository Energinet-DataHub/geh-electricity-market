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
using ElectricityMarket.Application.Commands.Contacts;
using ElectricityMarket.Application.Commands.GridArea;
using ElectricityMarket.Application.Models;
using ElectricityMarket.Domain.Models.GridArea;
using ElectricityMarket.Domain.Repositories;
using MediatR;

namespace ElectricityMarket.Application.Handlers;

public sealed class GetGridAreaOwnerHandler : IRequestHandler<GetGridAreaOwnerCommand, GridAreaOwnerDto>
{
    private readonly IMarketParticipantRepository _participantRepository;

    public GetGridAreaOwnerHandler(IMarketParticipantRepository participantRepository)
    {
        _participantRepository = participantRepository;
    }

    public async Task<GridAreaOwnerDto> Handle(GetGridAreaOwnerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var gridArea = await _participantRepository.GetGridAreaAsync(new GridAreaCode(request.GridAreaCode)).ConfigureAwait(false);

        if (gridArea == null)
            throw new ValidationException($"The grid area with code: {request.GridAreaCode} was not found");

        var domainEvents = await _participantRepository.GetGridAreaOwnershipAssignedEventsAsync().ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var gridAreaAssignedEvent = domainEvents.FirstOrDefault(x => x.GridAreaId == gridArea.Id);

        if (gridAreaAssignedEvent == null)
            throw new ValidationException($"No owner assigned for grid area: {gridArea.Id} was found");

        return new GridAreaOwnerDto(gridAreaAssignedEvent.ActorNumber.Value);
    }
}
