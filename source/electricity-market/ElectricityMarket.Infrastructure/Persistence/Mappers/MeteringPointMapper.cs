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

using System.Linq;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;

internal sealed class MeteringPointMapper
{
    public static MeteringPoint MapFromEntity(MeteringPointEntity from)
    {
        return new MeteringPoint(
            from.Id,
            new MeteringPointIdentification(from.Identification),
            from.MeteringPointPeriods.Select(MapFromEntity),
            from.CommercialRelations.Select(CommercialRelationMapper.MapFromEntity));
    }

    private static MeteringPointPeriod MapFromEntity(MeteringPointPeriodEntity from)
    {
        return new MeteringPointPeriod(
            from.Id,
            from.MeteringPointId,
            from.ValidFrom.ToInstant(),
            from.ValidTo.ToInstant(),
            from.CreatedAt.ToInstant(),
            from.GridAreaCode,
            from.OwnedBy,
            from.ConnectionState,
            from.Type,
            from.SubType,
            from.Resolution,
            from.Unit,
            from.ProductId,
            from.ScheduledMeterReadingMonth);
    }
}
