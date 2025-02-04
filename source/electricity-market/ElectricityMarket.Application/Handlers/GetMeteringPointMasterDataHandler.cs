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

using ElectricityMarket.Application.Commands.MasterData;
using ElectricityMarket.Application.Mappers;
using ElectricityMarket.Domain.Models;
using ElectricityMarket.Domain.Repositories;
using MediatR;

namespace ElectricityMarket.Application.Handlers;

public sealed class GetMeteringPointMasterDataHandler : IRequestHandler<GetMeteringPointMasterDataCommand, GetMeteringPointMasterDataResponse>
{
    private readonly IMeteringPointRepository _meteringPointRepository;

    public GetMeteringPointMasterDataHandler(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task<GetMeteringPointMasterDataResponse> Handle(GetMeteringPointMasterDataCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var masterData = await _meteringPointRepository.GetMeteringPointMasterDataChangesAsync(
            request.Request.MeteringPointIdentification,
            request.Request.StartDate,
            request.Request.EndDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GetMeteringPointMasterDataResponse(masterData.Select(MasterDataMapper.Map));
    }
}
