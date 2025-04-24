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

        return tenant.MarketRole switch
        {
            MarketRole.DataHubAdministrator or MarketRole.SystemOperator => meteringPoint,
            MarketRole.EnergySupplier => EnergySupplierFiltering(meteringPoint, tenant),
            MarketRole.GridAccessProvider => GridAccessProviderFiltering(meteringPoint, tenant),
            _ => null
        };
    }

    private static MeteringPointDto? GridAccessProviderFiltering(MeteringPointDto meteringPoint, TenantDto tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var haveBeenOwner = meteringPoint.MetadataTimeline.Any(x => x.OwnedBy == tenant.ActorNumber);

        if (!haveBeenOwner)
        {
            return null;
        }

        var mergedCommercialRelation = new CommercialRelationDto(
            -1,
            string.Empty,
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue,
            meteringPoint?.CommercialRelation?.ActiveEnergySupplyPeriod,
            meteringPoint?.CommercialRelationTimeline?.SelectMany(x => x.EnergySupplyPeriodTimeline) ?? [],
            null,
            []);

        ArgumentNullException.ThrowIfNull(meteringPoint);

        return meteringPoint with
        {
            CommercialRelation = mergedCommercialRelation,
            CommercialRelationTimeline = [mergedCommercialRelation],
        };
    }

    private static CommercialRelationDto RemoveEnergySupplierAndMergeEnergySupplyPeriods(
        CommercialRelationDto commercialRelation, IEnumerable<EnergySupplyPeriodDto> energySupplyPeriodTimeline)
    {
        return commercialRelation with
        {
            EnergySupplier = string.Empty,
            StartDate = DateTimeOffset.MinValue,
            EndDate = DateTimeOffset.MaxValue,
            EnergySupplyPeriodTimeline = energySupplyPeriodTimeline,
        };
    }

    private static MeteringPointDto EnergySupplierFiltering(MeteringPointDto meteringPoint, TenantDto tenant)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(tenant);

        var isActiveEnergySupplier = meteringPoint.CommercialRelation?.EnergySupplier == tenant.ActorNumber;

        var ownedCommercialRelations = meteringPoint.CommercialRelationTimeline
            .Where(x => x.EnergySupplier == tenant.ActorNumber)
            .ToList();

        var notOwnedCommercialRelations = meteringPoint.CommercialRelationTimeline
            .Where(x => x.EnergySupplier != tenant.ActorNumber)
            .Select(x => OnlyKeepCVRCustomerInfo(x))
            .ToList();

        var filteredMeteringPoint = meteringPoint with
        {
            CommercialRelationTimeline = ownedCommercialRelations.Concat(notOwnedCommercialRelations),
            CommercialRelation = !isActiveEnergySupplier && meteringPoint.CommercialRelation is not null ? OnlyKeepCVRCustomerInfo(meteringPoint.CommercialRelation) : meteringPoint.CommercialRelation,
        };

        return filteredMeteringPoint;
    }

    private static CommercialRelationDto OnlyKeepCVRCustomerInfo(CommercialRelationDto commercialRelation)
    {
        ArgumentNullException.ThrowIfNull(commercialRelation);

        if (commercialRelation.ActiveEnergySupplyPeriod is not null)
        {
            var cvrCustomer = commercialRelation.ActiveEnergySupplyPeriod.Customers.FirstOrDefault(c => !string.IsNullOrEmpty(c.Cvr));
            var replacementActiveEnergySupplyPeriod = commercialRelation.ActiveEnergySupplyPeriod with
            {
                ValidFrom = DateTimeOffset.MinValue,
                ValidTo = DateTimeOffset.MaxValue,
                Customers = cvrCustomer is null ? [] : [cvrCustomer with { IsProtectedName = false, LegalContact = null, TechnicalContact = null }]
            };

            return commercialRelation with
            {
                EnergySupplier = string.Empty,
                ActiveEnergySupplyPeriod = replacementActiveEnergySupplyPeriod,
                EnergySupplyPeriodTimeline = [replacementActiveEnergySupplyPeriod]
            };
        }

        return commercialRelation with
        {
            EnergySupplier = string.Empty,
            ActiveEnergySupplyPeriod = null,
            EnergySupplyPeriodTimeline = []
        };
    }
}
