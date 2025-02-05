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

using ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Models;

namespace ElectricityMarket.Application.Mappers;

internal sealed class MeteringPointMapper
{
    public static MeteringPointDto Map(MeteringPoint entity)
    {
        return new MeteringPointDto(
            entity.Id,
            entity.Identification.Value,
            entity.MeteringPointPeriods.Select(Map),
            []);
    }

    private static MeteringPointPeriodDto Map(MeteringPointPeriod meteringPointPeriodEntity)
    {
        return new MeteringPointPeriodDto(
            meteringPointPeriodEntity.Id,
            meteringPointPeriodEntity.ValidFrom.ToDateTimeOffset(),
            meteringPointPeriodEntity.ValidTo.ToDateTimeOffset(),
            meteringPointPeriodEntity.CreatedAt.ToDateTimeOffset(),
            meteringPointPeriodEntity.GridAreaCode,
            meteringPointPeriodEntity.OwnedBy,
            meteringPointPeriodEntity.ConnectionState,
            meteringPointPeriodEntity.Type,
            meteringPointPeriodEntity.SubType,
            meteringPointPeriodEntity.Resolution,
            meteringPointPeriodEntity.Unit,
            meteringPointPeriodEntity.ProductId,
            meteringPointPeriodEntity.ScheduledMeterReadingMonth,
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            null,
            40,
            50,
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            new InstallationAddressDto(
                1,
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD"),
            "TBD",
            "TBD");
    }
}
