﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Globalization;
using Energinet.DataHub.ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetMeteringPointByGridAreaCodeHandler : IRequestHandler<GetMeteringPointByGridAreaCodeCommand, GetMeteringPointByGridAreaCodeResponse>
{
    private readonly IMeteringPointRepository _meteringPointRepository;

    public GetMeteringPointByGridAreaCodeHandler(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task<GetMeteringPointByGridAreaCodeResponse> Handle(GetMeteringPointByGridAreaCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var meteringPoints = await _meteringPointRepository
            .GetByGridAreaCodeAsync(request.GridAreaCode)
            .ConfigureAwait(false);

        return new GetMeteringPointByGridAreaCodeResponse(meteringPoints.Select(x => new MeteringPointIdentificationDto(x.Value.ToString(CultureInfo.InvariantCulture))));
    }
}
