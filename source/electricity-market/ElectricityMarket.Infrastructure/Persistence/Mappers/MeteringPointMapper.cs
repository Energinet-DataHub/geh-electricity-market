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

using System;
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;

public static class MeteringPointMapper
{
    public static MeteringPoint MapFromEntity(MeteringPointEntity from)
    {
        ArgumentNullException.ThrowIfNull(from);
        return new MeteringPoint(
            from.Id,
            from.Version,
            new MeteringPointIdentification(from.Identification),
            from.MeteringPointPeriods.Where(mpp => mpp.RetiredBy == null && mpp.ValidFrom != mpp.ValidTo).Select(MapFromEntity).OrderBy(mpp => mpp.Valid.Start).ToList(),
            from.CommercialRelations.Where(cr => cr.EndDate > cr.StartDate).Select(MapFromEntity).OrderBy(cr => cr.Period.Start).ToList());
    }

    private static MeteringPointMetadata MapFromEntity(MeteringPointPeriodEntity from)
    {
        return new MeteringPointMetadata(
            from.Id,
            new Interval(from.ValidFrom.ToInstant(), from.ValidTo.ToInstant()),
            from.ParentIdentification != null ? new MeteringPointIdentification(from.ParentIdentification.Value) : null,
            MeteringPointEnumMapper.MapEntity(MeteringPointEnumMapper.MeteringPointTypes, from.Type),
            MeteringPointEnumMapper.MapEntity(MeteringPointEnumMapper.MeteringPointSubTypes, from.SubType),
            MeteringPointEnumMapper.MapEntity(MeteringPointEnumMapper.ConnectionStates, from.ConnectionState),
            from.Resolution,
            from.GridAreaCode,
            from.OwnedBy!,
            MeteringPointEnumMapper.MapOptionalEntity(MeteringPointEnumMapper.ConnectionTypes, from.ConnectionType),
            MeteringPointEnumMapper.MapOptionalEntity(MeteringPointEnumMapper.DisconnectionTypes, from.DisconnectionType),
            MeteringPointEnumMapper.MapEntity(MeteringPointEnumMapper.Products, from.Product),
            from.ProductObligation,
            MeteringPointEnumMapper.MapEntity(MeteringPointEnumMapper.MeasureUnits, from.MeasureUnit),
            MeteringPointEnumMapper.MapOptionalEntity(MeteringPointEnumMapper.AssetTypes, from.AssetType),
            from.FuelType,
            from.Capacity,
            from.PowerLimitKw,
            from.MeterNumber,
            from.SettlementGroup,
            from.ScheduledMeterReadingMonth,
            from.ExchangeFromGridArea,
            from.ExchangeToGridArea,
            from.PowerPlantGsrn,
            MeteringPointEnumMapper.MapOptionalEntity(MeteringPointEnumMapper.SettlementMethods, from.SettlementMethod),
            MapFromEntity(from.InstallationAddress),
            from.TransactionType);
    }

    private static InstallationAddress MapFromEntity(InstallationAddressEntity from)
    {
        return new InstallationAddress(
            from.Id,
            from.StreetCode,
            from.StreetName,
            from.BuildingNumber,
            from.CityName,
            from.CitySubdivisionName,
            from.DarReference,
            MeteringPointEnumMapper.MapOptionalEntity(MeteringPointEnumMapper.WashInstructionsTypes, from.WashInstructions),
            from.CountryCode,
            from.Floor,
            from.Room,
            from.PostCode,
            from.MunicipalityCode,
            from.LocationDescription);
    }

    private static CommercialRelation MapFromEntity(CommercialRelationEntity from)
    {
        return new CommercialRelation(
            from.Id,
            from.EnergySupplier,
            new Interval(from.StartDate.ToInstant(), from.EndDate.ToInstant()),
            from.ClientId,
            from.EnergySupplyPeriods.Where(esp => esp.RetiredBy == null).Select(MapFromEntity).OrderBy(esp => esp.Valid.Start).ToList(),
            from.ElectricalHeatingPeriods.Where(ehp => ehp.RetiredBy == null).Select(MapFromEntity).OrderBy(ehp => ehp.Period.Start).ToList());
    }

    private static ElectricalHeatingPeriod MapFromEntity(ElectricalHeatingPeriodEntity from)
    {
        return new ElectricalHeatingPeriod(
            from.Id,
            new Interval(from.ValidFrom.ToInstant(), from.ValidTo.ToInstant()),
            from.Active);
    }

    private static EnergySupplyPeriod MapFromEntity(EnergySupplyPeriodEntity from)
    {
        return new EnergySupplyPeriod(
            from.Id,
            new Interval(from.ValidFrom.ToInstant(), from.ValidTo.ToInstant()),
            from.Contacts.Select(MapFromEntity).ToList());
    }

    private static Customer MapFromEntity(ContactEntity from)
    {
        CustomerContact? legal = null;
        CustomerContact? technical = null;

        if (from.RelationType == "Contact4")
        {
            legal = new CustomerContact(
                from.Id,
                from.ContactName!,
                from.Email!,
                from.ContactAddresses.Single().IsProtectedAddress,
                from.Phone,
                from.Mobile,
                MapFromEntity(from.ContactAddresses.First()));
        }

        if (from.RelationType == "Contact1")
        {
            technical = new CustomerContact(
                from.Id,
                from.ContactName!,
                from.Email!,
                from.ContactAddresses.Single().IsProtectedAddress,
                from.Phone,
                from.Mobile,
                MapFromEntity(from.ContactAddresses.Single()));
        }

        return new Customer(
            from.Id,
            from.DisponentName,
            from.Cvr,
            from.Cpr,
            from.IsProtectedName,
            legal,
            technical);
    }

    private static CustomerContactAddress? MapFromEntity(ContactAddressEntity? from)
    {
        if (from == null)
            return null;

        return new CustomerContactAddress(
            from.Id,
            from.Attention,
            from.StreetCode,
            from.StreetName,
            from.BuildingNumber,
            from.CityName,
            from.CitySubdivisionName,
            from.DarReference,
            from.CountryCode,
            from.Floor,
            from.Room,
            from.PostCode,
            from.PostBox,
            from.MunicipalityCode);
    }
}
