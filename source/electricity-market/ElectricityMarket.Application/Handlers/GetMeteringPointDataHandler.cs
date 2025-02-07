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
using ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Application.Mappers;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetMeteringPointDataHandler : IRequestHandler<GetMeteringPointDataCommand, GetMeteringPointDataResponse>
{
    private readonly IMeteringPointRepository _meteringPointRepository;

    public GetMeteringPointDataHandler(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task<GetMeteringPointDataResponse> Handle(GetMeteringPointDataCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var meteringPointData = await _meteringPointRepository
            .GetAsync(new MeteringPointIdentification(request.MeteringPointIdentification))
            .ConfigureAwait(false);

        if (meteringPointData == null)
            throw new ValidationException($"Metering point id '{request.MeteringPointIdentification}' does not exists");

        return new GetMeteringPointDataResponse(MeteringPointMapper.Map(meteringPointData));
    }
}
