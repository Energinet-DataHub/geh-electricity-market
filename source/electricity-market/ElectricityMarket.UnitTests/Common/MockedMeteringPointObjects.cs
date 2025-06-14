﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Collections.Generic;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Common;

internal static class MockedMeteringPointObjects
{
    internal static MeteringPointDto GetMockedMeteringPoint(
        long id,
        string identification,
        MeteringPointMetadataDto metadata,
        IEnumerable<MeteringPointMetadataDto> metadataTimeline,
        CommercialRelationDto commercialRelation,
        IEnumerable<CommercialRelationDto> commercialRelationTimeline)
        => new(id, identification, metadata, metadataTimeline, commercialRelation, commercialRelationTimeline);

    internal static MeteringPointDto GetMockedMeteringPoint(long id, string ownedBy)
        => new(
            id,
            "Identification",
            GetMockedMeteringPointMetadata(1, ownedBy),
            [],
            GetMockedCommercialRelation(2, ownedBy),
            []);

    internal static MeteringPointMetadataDto GetMockedMeteringPointMetadata(long id, string ownedBy)
    => new(
        id,
        DateTimeOffset.Now.AddDays(-1),
        DateTimeOffset.Now.AddDays(1),
        "ParentMeteringPoint",
        MeteringPointType.Consumption,
        MeteringPointSubType.Calculated,
        ConnectionState.Connected,
        "Resolution",
        "GridAreaCode",
        ownedBy,
        ConnectionType.Direct,
        DisconnectionType.ManualDisconnection,
        Product.EnergyActive,
        false,
        MeteringPointMeasureUnit.KW,
        AssetType.GasTurbine,
        false,
        "Capacity",
        100,
        "MeterNumber",
        1,
        1,
        "FromGridAreaCode",
        "ToGridAreaCode",
        "PowerPlantGsrn",
        SettlementMethod.NonProfiled,
        GetMockedInstallationAddress(11));

    internal static InstallationAddressDto GetMockedInstallationAddress(long id)
        => new(id, "StreetCode", "StreetName", "BuildingNumber", "CityName", "CitySubDivisionName", Guid.NewGuid(), WashInstructions.Washable, "CountryCode", "Floor", "Room", "PostCode", "MunicipalityCode", "LocationDescription");

    internal static CommercialRelationDto GetMockedCommercialRelation(long id, string energySypplier)
        => new(
            id,
            energySypplier,
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddDays(1),
            GetMockedEnergySupplyPeriod(11),
            new List<EnergySupplyPeriodDto>(),
            GetMockedElectricalHeating(22),
            new List<ElectricalHeatingDto>());

    internal static EnergySupplyPeriodDto GetMockedEnergySupplyPeriod(long id)
        => new(id, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1), [GetMockedCustomer(111)]);

    internal static CustomerDto GetMockedCustomer(long id)
        => new(id, "Name", "Cvr", false, GetMockedCustomerContact(1111), GetMockedCustomerContact(1112));

    internal static CustomerContactDto GetMockedCustomerContact(long id)
        => new(id, "Name", "Email", false, "Phone", "Mobile", "Attention", "StreetCode", "StreetName", "BuildingNumber", "PostCode", "CityName", "CitySubdivision", Guid.NewGuid(), "CountryCode", "Floor", "Room", "PostBox", "MunicipalityCode");

    internal static ElectricalHeatingDto GetMockedElectricalHeating(long id)
        => new(id, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1), true);
}
