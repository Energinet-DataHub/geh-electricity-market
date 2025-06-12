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
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Application.Helpers.Timeline;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Services;

public class MeasurementsReportServiceTests
{
    private readonly MeasurementsReportService _sut = new();

    [Fact]
    public void Given_ParentAndChildren_When_GetMeasurementsReportIsCalled_Then_YieldExpectedMeasurementsReports()
    {
        // Arrange
        var parentId = new MeteringPointIdentification(1L);
        var childId = new MeteringPointIdentification(2L);
        var version = DateTimeOffset.UtcNow;

        var parentMetadata = CreateMeasurementsReportParentMetadata();
        var parentRelations = CreateMeasurementsReportParentCommercialRelations();

        var parentMp = new MeteringPoint(
            Id: 1L,
            Version: version,
            Identification: parentId,
            MetadataTimeline: parentMetadata,
            CommercialRelationTimeline: parentRelations);

        var childMetadata = CreateMeasurementsReportChildMetadata(parentId);

        var childMp = new MeteringPoint(
            Id: 2L,
            Version: version,
            Identification: childId,
            MetadataTimeline: childMetadata,
            CommercialRelationTimeline: []);

        var hierarchy = new MeteringPointHierarchy(
            Parent: parentMp,
            ChildMeteringPoints: [childMp],
            Version: version);

        // Act
        var reportDtos = _sut.GetMeasurementsReport(hierarchy).ToList();

        // Assert total count
        Assert.Equal(11, reportDtos.Count);

        // Parent DTOs (indices 0-5)
        var p0 = reportDtos[0];
        Assert.Equal(1L, p0.MeteringPointId);
        Assert.Equal("consumption", p0.MeteringPointType);
        Assert.Equal("111", p0.GridAreaCode);
        Assert.Equal("PT15M", p0.Resolution);
        Assert.Null(p0.EnergySupplierId);
        Assert.Equal("connected", p0.PhysicalStatus);
        Assert.Equal("KWh", p0.QuantityUnit);
        Assert.Null(p0.FromGridAreaCode);
        Assert.Null(p0.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 1, 1, 0, 0).ToDateTimeOffset(), p0.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 3, 1, 0, 0).ToDateTimeOffset(), p0.PeriodToDate);

        var p1 = reportDtos[1];
        Assert.Equal(1L, p1.MeteringPointId);
        Assert.Equal("consumption", p1.MeteringPointType);
        Assert.Equal("111", p1.GridAreaCode);
        Assert.Equal("PT15M", p1.Resolution);
        Assert.Equal("SupplierX", p1.EnergySupplierId);
        Assert.Equal("connected", p1.PhysicalStatus);
        Assert.Equal("KWh", p1.QuantityUnit);
        Assert.Null(p1.FromGridAreaCode);
        Assert.Null(p1.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 3, 1, 0, 0).ToDateTimeOffset(), p1.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 4, 1, 0, 0).ToDateTimeOffset(), p1.PeriodToDate);

        var p2 = reportDtos[2];
        Assert.Equal(1L, p2.MeteringPointId);
        Assert.Equal("consumption", p2.MeteringPointType);
        Assert.Equal("111", p2.GridAreaCode);
        Assert.Equal("PT15M", p2.Resolution);
        Assert.Equal("SupplierX", p2.EnergySupplierId);
        Assert.Equal("connected", p2.PhysicalStatus);
        Assert.Equal("KWh", p2.QuantityUnit);
        Assert.Null(p2.FromGridAreaCode);
        Assert.Null(p2.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 4, 1, 0, 0).ToDateTimeOffset(), p2.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 5, 1, 0, 0).ToDateTimeOffset(), p2.PeriodToDate);

        var p3 = reportDtos[3];
        Assert.Equal(1L, p3.MeteringPointId);
        Assert.Equal("consumption", p3.MeteringPointType);
        Assert.Equal("111", p3.GridAreaCode);
        Assert.Equal("PT15M", p3.Resolution);
        Assert.Equal("SupplierY", p3.EnergySupplierId);
        Assert.Equal("connected", p3.PhysicalStatus);
        Assert.Equal("KWh", p3.QuantityUnit);
        Assert.Null(p3.FromGridAreaCode);
        Assert.Null(p3.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 5, 1, 0, 0).ToDateTimeOffset(), p3.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 6, 1, 0, 0).ToDateTimeOffset(), p3.PeriodToDate);

        var p4 = reportDtos[4];
        Assert.Equal(1L, p4.MeteringPointId);
        Assert.Equal("consumption", p4.MeteringPointType);
        Assert.Equal("111", p4.GridAreaCode);
        Assert.Equal("PT15M", p4.Resolution);
        Assert.Equal("SupplierY", p4.EnergySupplierId);
        Assert.Equal("connected", p4.PhysicalStatus);
        Assert.Equal("KWh", p4.QuantityUnit);
        Assert.Null(p4.FromGridAreaCode);
        Assert.Null(p4.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 6, 1, 0, 0).ToDateTimeOffset(), p4.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 7, 1, 0, 0).ToDateTimeOffset(), p4.PeriodToDate);

        var p5 = reportDtos[5];
        Assert.Equal(1L, p5.MeteringPointId);
        Assert.Equal("consumption", p5.MeteringPointType);
        Assert.Equal("111", p5.GridAreaCode);
        Assert.Equal("PT15M", p5.Resolution);
        Assert.Equal("SupplierZ", p5.EnergySupplierId);
        Assert.Equal("connected", p5.PhysicalStatus);
        Assert.Equal("KWh", p5.QuantityUnit);
        Assert.Null(p5.FromGridAreaCode);
        Assert.Null(p5.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 7, 1, 0, 0).ToDateTimeOffset(), p5.PeriodFromDate);
        Assert.Null(p5.PeriodToDate);

        // Child DTOs (indices 6-10)
        var c0 = reportDtos[6];
        Assert.Equal(2L, c0.MeteringPointId);
        Assert.Equal("net_consumption", c0.MeteringPointType);
        Assert.Equal("111", c0.GridAreaCode);
        Assert.Equal("PT15M", c0.Resolution);
        Assert.Null(c0.EnergySupplierId);
        Assert.Equal("connected", c0.PhysicalStatus);
        Assert.Equal("KWh", c0.QuantityUnit);
        Assert.Null(c0.FromGridAreaCode);
        Assert.Null(c0.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 2, 1, 0, 0).ToDateTimeOffset(), c0.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 3, 1, 0, 0).ToDateTimeOffset(), c0.PeriodToDate);

        var c1 = reportDtos[7];
        Assert.Equal(2L, c1.MeteringPointId);
        Assert.Equal("net_consumption", c1.MeteringPointType);
        Assert.Equal("111", c1.GridAreaCode);
        Assert.Equal("PT15M", c1.Resolution);
        Assert.Equal("SupplierX", c1.EnergySupplierId);
        Assert.Equal("connected", c1.PhysicalStatus);
        Assert.Equal("KWh", c1.QuantityUnit);
        Assert.Null(c1.FromGridAreaCode);
        Assert.Null(c1.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 3, 1, 0, 0).ToDateTimeOffset(), c1.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 5, 1, 0, 0).ToDateTimeOffset(), c1.PeriodToDate);

        var c2 = reportDtos[8];
        Assert.Equal(2L, c2.MeteringPointId);
        Assert.Equal("net_consumption", c2.MeteringPointType);
        Assert.Equal("111", c2.GridAreaCode);
        Assert.Equal("PT15M", c2.Resolution);
        Assert.Equal("SupplierY", c2.EnergySupplierId);
        Assert.Equal("connected", c2.PhysicalStatus);
        Assert.Equal("KWh", c2.QuantityUnit);
        Assert.Null(c2.FromGridAreaCode);
        Assert.Null(c2.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 5, 1, 0, 0).ToDateTimeOffset(), c2.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 5, 15, 0, 0).ToDateTimeOffset(), c2.PeriodToDate);

        var c3 = reportDtos[9];
        Assert.Equal(2L, c3.MeteringPointId);
        Assert.Equal("net_consumption", c3.MeteringPointType);
        Assert.Equal("111", c3.GridAreaCode);
        Assert.Equal("PT15M", c3.Resolution);
        Assert.Equal("SupplierY", c3.EnergySupplierId);
        Assert.Equal("connected", c3.PhysicalStatus);
        Assert.Equal("KWh", c3.QuantityUnit);
        Assert.Null(c3.FromGridAreaCode);
        Assert.Null(c3.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 5, 15, 0, 0).ToDateTimeOffset(), c3.PeriodFromDate);
        Assert.Equal(Instant.FromUtc(2021, 7, 1, 0, 0).ToDateTimeOffset(), c3.PeriodToDate);

        var c4 = reportDtos[10];
        Assert.Equal(2L, c4.MeteringPointId);
        Assert.Equal("net_consumption", c4.MeteringPointType);
        Assert.Equal("111", c4.GridAreaCode);
        Assert.Equal("PT15M", c4.Resolution);
        Assert.Equal("SupplierZ", c4.EnergySupplierId);
        Assert.Equal("connected", c4.PhysicalStatus);
        Assert.Equal("KWh", c4.QuantityUnit);
        Assert.Null(c4.FromGridAreaCode);
        Assert.Null(c4.ToGridAreaCode);
        Assert.Equal(Instant.FromUtc(2021, 7, 1, 0, 0).ToDateTimeOffset(), c4.PeriodFromDate);
        Assert.Null(c4.PeriodToDate);
    }

    private static List<MeteringPointMetadata> CreateMeasurementsReportChildMetadata(MeteringPointIdentification parentId)
    {
        // -- Child metadata: CM1(02-01→05-15), CM2(05-15→∞)
        return new List<MeteringPointMetadata>
        {
            new MeteringPointMetadata(
                Id: 301,
                Valid: new Interval(Instant.FromUtc(2021, 2, 1, 0, 0), Instant.FromUtc(2021, 5, 15, 0, 0)),
                Parent: parentId,
                Type: MeteringPointType.NetConsumption,
                SubType: MeteringPointSubType.Virtual,
                ConnectionState: ConnectionState.Connected,
                Resolution: "PT15M",
                GridAreaCode: "111",
                OwnedBy: "OwnerX",
                ConnectionType: null,
                DisconnectionType: null,
                Product: Product.EnergyActive,
                ProductObligation: false,
                MeasureUnit: MeteringPointMeasureUnit.KWh,
                AssetType: null,
                EnvironmentalFriendly: false,
                Capacity: null,
                PowerLimitKw: null,
                MeterNumber: null,
                NetSettlementGroup: 6,
                ScheduledMeterReadingMonth: 5,
                ExchangeFromGridAreaCode: null,
                ExchangeToGridAreaCode: null,
                PowerPlantGsrn: null,
                SettlementMethod: null,
                InstallationAddress: new InstallationAddress(
                    Id: 601,
                    StreetCode: "SC601",
                    StreetName: "Elm Street",
                    BuildingNumber: "10",
                    CityName: "Copenhagen",
                    CitySubDivisionName: null,
                    DarReference: Guid.Parse("42345678-1234-1234-1234-1234567890ab"),
                    WashInstructions: null,
                    CountryCode: "DK",
                    Floor: "Ground",
                    Room: null,
                    PostCode: "1050",
                    MunicipalityCode: "0101",
                    LocationDescription: "Basement meter"),
                TransactionType: "MOVEINES"),

            new MeteringPointMetadata(
                Id: 302,
                Valid: new Interval(Instant.FromUtc(2021, 5, 15, 0, 0), Instant.MaxValue),
                Parent: parentId,
                Type: MeteringPointType.NetConsumption,
                SubType: MeteringPointSubType.Physical,
                ConnectionState: ConnectionState.Connected,
                Resolution: "PT15M",
                GridAreaCode: "111",
                OwnedBy: "OwnerX",
                ConnectionType: null,
                DisconnectionType: null,
                Product: Product.EnergyActive,
                ProductObligation: false,
                MeasureUnit: MeteringPointMeasureUnit.KWh,
                AssetType: null,
                EnvironmentalFriendly: false,
                Capacity: null,
                PowerLimitKw: null,
                MeterNumber: null,
                NetSettlementGroup: 6,
                ScheduledMeterReadingMonth: 5,
                ExchangeFromGridAreaCode: null,
                ExchangeToGridAreaCode: null,
                PowerPlantGsrn: null,
                SettlementMethod: null,
                InstallationAddress: new InstallationAddress(
                    Id: 602,
                    StreetCode: "SC602",
                    StreetName: "Maple Avenue",
                    BuildingNumber: "22B",
                    CityName: "Copenhagen",
                    CitySubDivisionName: "City Center",
                    DarReference: Guid.Parse("52345678-1234-1234-1234-1234567890ab"),
                    WashInstructions: null,
                    CountryCode: "DK",
                    Floor: "1",
                    Room: "101",
                    PostCode: "1000",
                    MunicipalityCode: "0101",
                    LocationDescription: "First floor meter closet"),
                TransactionType: "MOVED"),
        };
    }

    private static List<CommercialRelation> CreateMeasurementsReportParentCommercialRelations()
    {
        // -- Parent relations: X(03-01→05-01), Y(05-01→07-01), Z(07-01→∞)
        return new List<CommercialRelation>
            {
                new CommercialRelation(
                    Id: 201,
                    EnergySupplier: "SupplierX",
                    Period: new Interval(Instant.FromUtc(2021, 3, 1, 0, 0), Instant.FromUtc(2021, 5, 1, 0, 0)),
                    ClientId: null,
                    EnergySupplyPeriodTimeline: [],
                    ElectricalHeatingPeriods: []),

                new CommercialRelation(
                    Id: 202,
                    EnergySupplier: "SupplierY",
                    Period: new Interval(Instant.FromUtc(2021, 5, 1, 0, 0), Instant.FromUtc(2021, 7, 1, 0, 0)),
                    ClientId: null,
                    EnergySupplyPeriodTimeline: [],
                    ElectricalHeatingPeriods: []),

                new CommercialRelation(
                    Id: 203,
                    EnergySupplier: "SupplierZ",
                    Period: new Interval(Instant.FromUtc(2021, 7, 1, 0, 0), Instant.MaxValue),
                    ClientId: null,
                    EnergySupplyPeriodTimeline: [],
                    ElectricalHeatingPeriods: []),
            };
    }

    private static List<MeteringPointMetadata> CreateMeasurementsReportParentMetadata()
    {
        // -- Parent metadata: MetaA(2021-01-01→2021-04-01), MetaB(2021-04-01→2021-06-01), MetaC(2021-06-01→∞)
        return new List<MeteringPointMetadata>
        {
            new MeteringPointMetadata(
                Id: 101,
                Valid: new Interval(Instant.FromUtc(2021, 1, 1, 0, 0), Instant.FromUtc(2021, 4, 1, 0, 0)),
                Parent: null,
                Type: MeteringPointType.Consumption,
                SubType: MeteringPointSubType.Physical,
                ConnectionState: ConnectionState.Connected,
                Resolution: "PT15M",
                GridAreaCode: "111",
                OwnedBy: "OwnerX",
                ConnectionType: null,
                DisconnectionType: null,
                Product: Product.EnergyActive,
                ProductObligation: false,
                MeasureUnit: MeteringPointMeasureUnit.KWh,
                AssetType: null,
                EnvironmentalFriendly: false,
                Capacity: null,
                PowerLimitKw: null,
                MeterNumber: null,
                NetSettlementGroup: 6,
                ScheduledMeterReadingMonth: 1,
                ExchangeFromGridAreaCode: null,
                ExchangeToGridAreaCode: null,
                PowerPlantGsrn: null,
                SettlementMethod: null,
                InstallationAddress: new InstallationAddress(
                    Id: 501,
                    StreetCode: "SC100",
                    StreetName: "Main Street",
                    BuildingNumber: "42A",
                    CityName: "Copenhagen",
                    CitySubDivisionName: null,
                    DarReference: Guid.Parse("12345678-1234-1234-1234-1234567890ab"),
                    WashInstructions: null,
                    CountryCode: "DK",
                    Floor: "2",
                    Room: "201",
                    PostCode: "1000",
                    MunicipalityCode: "0101",
                    LocationDescription: "Near the harbor"),
                TransactionType: "MSTDATSBM"),

            new MeteringPointMetadata(
                Id: 102,
                Valid: new Interval(Instant.FromUtc(2021, 4, 1, 0, 0), Instant.FromUtc(2021, 6, 1, 0, 0)),
                Parent: null,
                Type: MeteringPointType.Consumption,
                SubType: MeteringPointSubType.Physical,
                ConnectionState: ConnectionState.Connected,
                Resolution: "PT15M",
                GridAreaCode: "111",
                OwnedBy: "OwnerX",
                ConnectionType: null,
                DisconnectionType: null,
                Product: Product.EnergyActive,
                ProductObligation: false,
                MeasureUnit: MeteringPointMeasureUnit.KWh,
                AssetType: null,
                EnvironmentalFriendly: false,
                Capacity: null,
                PowerLimitKw: null,
                MeterNumber: null,
                NetSettlementGroup: 6,
                ScheduledMeterReadingMonth: 4,
                ExchangeFromGridAreaCode: null,
                ExchangeToGridAreaCode: null,
                PowerPlantGsrn: null,
                SettlementMethod: null,
                InstallationAddress: new InstallationAddress(
                    Id: 502,
                    StreetCode: "SC200",
                    StreetName: "High Street",
                    BuildingNumber: "7B",
                    CityName: "Copenhagen",
                    CitySubDivisionName: "Downtown",
                    DarReference: Guid.Parse("22345678-1234-1234-1234-1234567890ab"),
                    WashInstructions: null,
                    CountryCode: "DK",
                    Floor: "1",
                    Room: "101",
                    PostCode: "1050",
                    MunicipalityCode: "0101",
                    LocationDescription: "Next to the station"),
                TransactionType: "CHANGESUP"),

            new MeteringPointMetadata(
                Id: 103,
                Valid: new Interval(Instant.FromUtc(2021, 6, 1, 0, 0), Instant.MaxValue),
                Parent: null,
                Type: MeteringPointType.Consumption,
                SubType: MeteringPointSubType.Physical,
                ConnectionState: ConnectionState.Connected,
                Resolution: "PT15M",
                GridAreaCode: "111",
                OwnedBy: "OwnerX",
                ConnectionType: null,
                DisconnectionType: null,
                Product: Product.EnergyActive,
                ProductObligation: false,
                MeasureUnit: MeteringPointMeasureUnit.KWh,
                AssetType: null,
                EnvironmentalFriendly: false,
                Capacity: null,
                PowerLimitKw: null,
                MeterNumber: null,
                NetSettlementGroup: 6,
                ScheduledMeterReadingMonth: 6,
                ExchangeFromGridAreaCode: null,
                ExchangeToGridAreaCode: null,
                PowerPlantGsrn: null,
                SettlementMethod: null,
                InstallationAddress: new InstallationAddress(
                    Id: 503,
                    StreetCode: "SC300",
                    StreetName: "Harbor Road",
                    BuildingNumber: "3C",
                    CityName: "Copenhagen",
                    CitySubDivisionName: null,
                    DarReference: Guid.Parse("32345678-1234-1234-1234-1234567890ab"),
                    WashInstructions: null,
                    CountryCode: "DK",
                    Floor: null,
                    Room: null,
                    PostCode: "1100",
                    MunicipalityCode: "0101",
                    LocationDescription: "Pier 3"),
                TransactionType: "MSTDATSBM"),
        };
    }
}
