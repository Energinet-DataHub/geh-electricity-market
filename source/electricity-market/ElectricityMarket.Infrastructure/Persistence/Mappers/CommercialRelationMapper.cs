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
using ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;

internal sealed class CommercialRelationMapper
{
    public static CommercialRelation MapFromEntity(CommercialRelationEntity from)
    {
        return new CommercialRelation(
            from.Id,
            from.CustomerId,
            from.MeteringPointId,
            from.StartDate.ToInstant(),
            from.EndDate.ToInstant(),
            from.EnergySupplier,
            from.ModifiedAt.ToInstant(),
            from.EnergySupplyPeriods.Select(MapFromEntity).ToList());
    }

    public static EnergySupplierPeriod MapFromEntity(EnergySupplyPeriodEntity from)
    {
        return new EnergySupplierPeriod(
            from.Id,
            from.ValidFrom.ToInstant(),
            from.ValidTo.ToInstant(),
            from.RetiredAt?.ToInstant(),
            from.RetiredById,
            from.BusinessTransactionDosId,
            from.WebAccessCode,
            from.EnergySupplier);
    }
}
