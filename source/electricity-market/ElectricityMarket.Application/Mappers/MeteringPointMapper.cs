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

namespace Energinet.DataHub.ElectricityMarket.Application.Mappers;

internal static class MeteringPointMapper
{
    public static MeteringPointDto Map(MeteringPoint meteringPoint)
    {
        var cr = meteringPoint.CommercialRelation;
        return new MeteringPointDto(
            meteringPoint.Id,
            meteringPoint.Identification.Value,
            Map(meteringPoint.Metadata),
            meteringPoint.MetadataTimeline.Select(Map),
            cr != null ? Map(cr) : null,
            meteringPoint.CommercialRelationTimeline.Select(Map));
    }

    private static MeteringPointMetadataDto Map(MeteringPointMetadata meteringPointMetadata)
    {
        return new MeteringPointMetadataDto(
            meteringPointMetadata.Id,
            meteringPointMetadata.Valid.Start.ToDateTimeOffset(),
            meteringPointMetadata.Valid.End.ToDateTimeOffset(),
            meteringPointMetadata.Parent?.Value,
            meteringPointMetadata.Type,
            meteringPointMetadata.SubType,
            meteringPointMetadata.ConnectionState,
            meteringPointMetadata.Resolution,
            meteringPointMetadata.GridAreaCode,
            meteringPointMetadata.OwnedBy,
            meteringPointMetadata.ConnectionType,
            meteringPointMetadata.DisconnectionType,
            meteringPointMetadata.Product,
            meteringPointMetadata.ProductObligation,
            meteringPointMetadata.MeasureUnit,
            meteringPointMetadata.AssetType,
            meteringPointMetadata.EnvironmentalFriendly,
            meteringPointMetadata.Capacity,
            meteringPointMetadata.PowerLimitKw,
            meteringPointMetadata.MeterNumber,
            meteringPointMetadata.NetSettlementGroup,
            meteringPointMetadata.ScheduledMeterReadingMonth,
            meteringPointMetadata.ExchangeFromGridAreaCode,
            meteringPointMetadata.ExchangeToGridAreaCode,
            meteringPointMetadata.PowerPlantGsrn,
            meteringPointMetadata.SettlementMethod,
            Map(meteringPointMetadata.InstallationAddress));
    }

    private static InstallationAddressDto Map(InstallationAddress installationAddress)
    {
        return new InstallationAddressDto(
            installationAddress.Id,
            installationAddress.StreetCode,
            installationAddress.StreetName,
            installationAddress.BuildingNumber,
            installationAddress.CityName,
            installationAddress.CitySubDivisionName,
            installationAddress.DarReference,
            installationAddress.WashInstructions,
            installationAddress.CountryCode,
            installationAddress.Floor,
            installationAddress.Room,
            installationAddress.PostCode,
            installationAddress.MunicipalityCode,
            installationAddress.LocationDescription);
    }

    private static CommercialRelationDto Map(CommercialRelation commercialRelation)
    {
        var now = DateTimeOffset.UtcNow;
        var espTimeline = commercialRelation.EnergySupplyPeriodTimeline.Select(Map).ToList();
        var heatingPeriods = commercialRelation.ElectricalHeatingPeriods.Select(Map).ToList();

        return new CommercialRelationDto(
            commercialRelation.Id,
            commercialRelation.EnergySupplier,
            commercialRelation.Period.Start.ToDateTimeOffset(),
            commercialRelation.Period.End.ToDateTimeOffset(),
            espTimeline.FirstOrDefault(esp => esp.ValidFrom <= now && esp.ValidTo > now),
            espTimeline,
            heatingPeriods.FirstOrDefault(hp => hp.ValidFrom <= now && hp.ValidTo > now),
            heatingPeriods);
    }

    private static ElectricalHeatingDto Map(ElectricalHeatingPeriod electricalHeatingPeriod)
    {
        return new ElectricalHeatingDto(
            electricalHeatingPeriod.Id,
            electricalHeatingPeriod.Period.Start.ToDateTimeOffset(),
            electricalHeatingPeriod.Period.End.ToDateTimeOffset());
    }

    private static EnergySupplyPeriodDto Map(EnergySupplyPeriod energySupplyPeriod)
    {
        return new EnergySupplyPeriodDto(
            energySupplyPeriod.Id,
            energySupplyPeriod.Valid.Start.ToDateTimeOffset(),
            energySupplyPeriod.Valid.End.ToDateTimeOffset(),
            energySupplyPeriod.Customers.Select(Map));
    }

    private static CustomerDto Map(Customer customer)
    {
        return new CustomerDto(
            customer.Id,
            customer.Name,
            customer.Cvr,
            customer.IsProtectedName,
            Map(customer.LegalContact),
            Map(customer.TechnicalContact));
    }

    private static CustomerContactDto? Map(CustomerContact? customerContact)
    {
        if (customerContact == null)
            return null;

        return new CustomerContactDto(
            customerContact.Id,
            customerContact.Name,
            customerContact.Email,
            customerContact.IsProtectedAddress,
            customerContact.Phone,
            customerContact.Mobile,
            customerContact.Address?.Attention,
            customerContact.Address?.StreetCode,
            customerContact.Address?.StreetName,
            customerContact.Address?.BuildingNumber,
            customerContact.Address?.PostCode,
            customerContact.Address?.CityName,
            customerContact.Address?.CitySubdivisionName,
            customerContact.Address?.DarReference,
            customerContact.Address?.CountryCode,
            customerContact.Address?.Floor,
            customerContact.Address?.Room,
            customerContact.Address?.PostBox,
            customerContact.Address?.MunicipalityCode);
    }
}
