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
using Energinet.DataHub.ElectricityMarket.Application.Security;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class RoleFiltrationService : IRoleFiltrationService
{
    public MeteringPointDto? FilterFields(MeteringPointDto meteringPoint, TenantDto tenant)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint, nameof(meteringPoint));
        ArgumentNullException.ThrowIfNull(tenant, nameof(tenant));
        var energySupplierNumber = meteringPoint.CommercialRelation?.EnergySupplier;

        return tenant.MarketRole switch
        {
            MarketRole.DataHubAdministrator => meteringPoint,
            MarketRole.EnergySupplier when energySupplierNumber == tenant.ActorNumber => meteringPoint,
            MarketRole.GridAccessProvider => GridAccessProviderFiltering(meteringPoint),
            MarketRole.EnergySupplier when energySupplierNumber != tenant.ActorNumber => EnergySupplierFiltering(meteringPoint),
            _ => null
        };
    }

    private static MeteringPointDto GridAccessProviderFiltering(MeteringPointDto meteringPoint)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        if (meteringPoint.CommercialRelation != null)
        {
            EnergySupplyPeriodDto? replacementActiveEnergySupplyPeriod = null;
            if (meteringPoint.CommercialRelation.ActiveEnergySupplyPeriod != null)
            {
                replacementActiveEnergySupplyPeriod = meteringPoint.CommercialRelation.ActiveEnergySupplyPeriod with
                {
                    ValidFrom = DateTimeOffset.MinValue,
                    ValidTo = DateTimeOffset.MaxValue
                };
            }

            return meteringPoint with
            {
                CommercialRelation = meteringPoint.CommercialRelation with
                {
                    EnergySupplier = string.Empty,
                    ActiveEnergySupplyPeriod = replacementActiveEnergySupplyPeriod,
                    EnergySupplyPeriodTimeline = []
                }
            };
        }

        return meteringPoint;
    }

    private static MeteringPointDto EnergySupplierFiltering(MeteringPointDto meteringPoint)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        if (meteringPoint.CommercialRelation != null)
        {
            EnergySupplyPeriodDto? replacementActiveEnergySupplyPeriod = null;
            if (meteringPoint.CommercialRelation.ActiveEnergySupplyPeriod != null)
            {
                var cvrCustomer = meteringPoint.CommercialRelation.ActiveEnergySupplyPeriod.Customers.FirstOrDefault(c => !string.IsNullOrEmpty(c.Cvr));
                replacementActiveEnergySupplyPeriod = meteringPoint.CommercialRelation.ActiveEnergySupplyPeriod with
                {
                    ValidFrom = DateTimeOffset.MinValue,
                    ValidTo = DateTimeOffset.MaxValue,
                    Customers = cvrCustomer is null ? [] : [cvrCustomer with { IsProtectedName = false, LegalContact = null, TechnicalContact = null }]
                };
            }

            return meteringPoint with
            {
                CommercialRelation = meteringPoint.CommercialRelation with
                {
                    EnergySupplier = string.Empty,
                    ActiveEnergySupplyPeriod = replacementActiveEnergySupplyPeriod,
                    EnergySupplyPeriodTimeline = []
                }
            };
        }

        return meteringPoint;
    }
}
