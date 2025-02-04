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

using ElectricityMarket.Application.Models;
using ElectricityMarket.Domain.Models;
using ElectricityMarket.Domain.Models.MasterData;


namespace ElectricityMarket.Application.Mappers;

internal sealed class MasterDataMapper
{
    public static MeteringPointMasterDataDto Map(MeteringPointMasterData entity)
    {
        return new MeteringPointMasterDataDto(
            entity.Identification.Value,
            entity.ValidFrom.ToDateTimeOffset(),
            entity.ValidTo.ToDateTimeOffset(),
            entity.GridAreaCode.Value,
            entity.GridAccessProvider.Value,
            entity.NeighborGridAreaOwners.Select(x => x.Value).ToList(),
            entity.ConnectionState,
            entity.Type,
            entity.SubType,
            entity.Resolution.Value,
            entity.Unit,
            entity.ProductId,
            entity.ParentIdentification?.Value);
    }
}
