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
using Energinet.DataHub.ElectricityMarket.Application.Commands.GridArea;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Models.GridAreas;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.GridAreas;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetGridAreaOwnerHandler : IRequestHandler<GetGridAreaOwnerCommand, GridAreaOwnerDto?>
{
    private readonly IActorRepository _actorRepository;
    private readonly IGridAreaRepository _gridAreaRepository;

    public GetGridAreaOwnerHandler(IGridAreaRepository participantRepository, IActorRepository actorRepository)
    {
        _gridAreaRepository = participantRepository;
        _actorRepository = actorRepository;
    }

    public async Task<GridAreaOwnerDto?> Handle(GetGridAreaOwnerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var gridArea = await _gridAreaRepository.GetGridAreaAsync(new GridAreaCode(request.GridAreaCode)).ConfigureAwait(false);

        if (gridArea == null)
            return null;

        var actors = await _actorRepository.GetActorsAsync().ConfigureAwait(false);

        var lookup = actors.Where(x => x.MarketRole.Function == EicFunction.GridAccessProvider && x.MarketRole.GridAreas.Count != 0)
            .SelectMany(x => x.MarketRole.GridAreas.Select(xx => new { xx.Id, x.ActorNumber })).ToLookup(x => x.Id, x => x.ActorNumber);

        var actorOwner = lookup[gridArea.Id].FirstOrDefault();

        if (actorOwner == null)
            throw new ValidationException($"No owner assigned for grid area: {gridArea.Id} was found");

        return new GridAreaOwnerDto(actorOwner.Value);
    }
}
