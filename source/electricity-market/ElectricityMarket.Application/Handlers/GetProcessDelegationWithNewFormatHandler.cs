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

using Energinet.DataHub.ElectricityMarket.Application.Commands.ProcessDelegations;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Integration.Models.ProcessDelegation;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetProcessDelegationWithNewFormatHandler : IRequestHandler<GetProcessDelegationCondensedCommand, ProcessDelegationDto?>
{
    private readonly IProcessDelegationCondensedRepository _processDelegationRepository;

    public GetProcessDelegationWithNewFormatHandler(IProcessDelegationCondensedRepository processDelegationRepository)
    {
        _processDelegationRepository = processDelegationRepository;
    }

    public async Task<ProcessDelegationDto?> Handle(GetProcessDelegationCondensedCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        return await _processDelegationRepository
            .GetProcessDelegationAsync(request.ProcessDelegationRequest)
            .ConfigureAwait(false);
    }
}
