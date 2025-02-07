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

using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Models;

namespace Energinet.DataHub.ElectricityMarket.Application.Mappers;

internal sealed class CommercialRelationMapper
{
    public static CommercialRelationDto Map(CommercialRelation entity)
    {
        return new CommercialRelationDto(
            entity.Id,
            entity.CustomerId,
            entity.MeteringPointId,
            entity.StartDate.ToDateTimeOffset(),
            entity.EndDate.ToDateTimeOffset(),
            entity.EnergySupplier,
            entity.ModifiedAt.ToDateTimeOffset(),
            entity.EnergySupplyPeriods.Select(Map),
            entity.ElectricalHeatingPeriods.Select(Map));
    }

    public static EnergySupplierPeriodDto Map(EnergySupplierPeriod periodEntity)
    {
        return new EnergySupplierPeriodDto(
            periodEntity.Id,
            periodEntity.ValidFrom.ToDateTimeOffset(),
            periodEntity.ValidTo.ToDateTimeOffset(),
            periodEntity.RetiredAt?.ToDateTimeOffset(),
            periodEntity.RetiredById,
            periodEntity.BusinessTransactionDosId,
            periodEntity.WebAccessCode,
            periodEntity.EnergySupplier,
            []);
    }

    public static ElectricalHeatingPeriodDto Map(ElectricalHeatingPeriod periodEntity)
    {
        return new ElectricalHeatingPeriodDto(
            periodEntity.Id,
            periodEntity.ValidFrom.ToDateTimeOffset(),
            periodEntity.ValidTo.ToDateTimeOffset(),
            periodEntity.RetiredAt?.ToDateTimeOffset(),
            periodEntity.RetiredById,
            periodEntity.BusinessTransactionDosId,
            periodEntity.TransactionType);
    }
}
