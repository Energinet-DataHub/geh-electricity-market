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

using Energinet.DataHub.ElectricityMarket.Application.Commands.Authorize;
using Energinet.DataHub.ElectricityMarket.Application.Commands.GridArea;
using Energinet.DataHub.ElectricityMarket.Application.Commands.MasterData;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Mappers;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.GridAreas;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class VerifyGridOwnerHandler : IRequestHandler<VerifyGridOwnerCommand, bool>
{
    private readonly IMeteringPointIntegrationRepository _meteringPointIntegrationRepository;

    public VerifyGridOwnerHandler(IMeteringPointIntegrationRepository meteringPointIntegrationRepository)
    {
        _meteringPointIntegrationRepository = meteringPointIntegrationRepository;
    }

    public async Task<bool> Handle(VerifyGridOwnerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var result = await _meteringPointIntegrationRepository.GetMeteringPointMasterDataChangesAsync(
            request.MeteringPointIdentification,
            DateTime.UtcNow,
            DateTime.UtcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // How do we get only the current state (like used in GUI meteringpoint)
        var first = result.FirstOrDefault<MeteringPointMasterData>();

        if (first == null)
        {
            return false;
        }

        // Can we use accessGridProvider (instead of list of gridareas)
        return request.GridAreaCodes.Contains(first.GridAreaCode.Value);
    }
}
