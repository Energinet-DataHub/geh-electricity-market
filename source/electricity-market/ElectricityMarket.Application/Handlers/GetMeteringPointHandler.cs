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

using Energinet.DataHub.ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Application.Mappers;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetMeteringPointHandler : IRequestHandler<GetMeteringPointCommand, GetMeteringPointResponse?>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly IRoleFiltrationService _roleFiltrationService;

    public GetMeteringPointHandler(IMeteringPointRepository meteringPointRepository, IRoleFiltrationService roleFiltrationService)
    {
        _meteringPointRepository = meteringPointRepository;
        _roleFiltrationService = roleFiltrationService;
    }

    public async Task<GetMeteringPointResponse?> Handle(GetMeteringPointCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var meteringPoint = await _meteringPointRepository
            .GetAsync(new MeteringPointIdentification(request.Identification))
            .ConfigureAwait(false);

        if (meteringPoint == null)
        {
            return null;
        }

        var filteredMeteringPoint = _roleFiltrationService.FilterFields(MeteringPointMapper.Map(meteringPoint), request.Tenant);

        if (filteredMeteringPoint == null)
        {
            return null;
        }

        return new GetMeteringPointResponse(filteredMeteringPoint);
    }
}
