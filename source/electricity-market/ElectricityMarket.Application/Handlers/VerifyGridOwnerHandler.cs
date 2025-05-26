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
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class VerifyGridOwnerHandler : IRequestHandler<VerifyGridOwnerCommand, bool>
{
    private readonly IMeteringPointRepository _meteringPointRepository;

    public VerifyGridOwnerHandler(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task<bool> Handle(VerifyGridOwnerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var result = await _meteringPointRepository.GetMeteringPointForSignatureAsync(
           new Domain.Models.MeteringPointIdentification(request.MeteringPointIdentification))
            .ConfigureAwait(false);

        if (result == null)
        {
            return false;
        }

        return request.GridAreaCodes.Contains(result.Metadata.GridAreaCode);
    }
}
