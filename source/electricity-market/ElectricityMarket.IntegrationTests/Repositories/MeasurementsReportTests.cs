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
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Helpers.Timeline;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Repositories;

public class MeasurementsReportTests : IClassFixture<ElectricityMarketDatabaseContextFixture>, IAsyncLifetime
{
    private readonly ElectricityMarketDatabaseContextFixture _fixture;

    public MeasurementsReportTests(ElectricityMarketDatabaseContextFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public void Given_ParentAndChild_When_BuildMergedTimelineIsCalled_Then_YieldExpectedGapFreeSlices()
    {
        // Arrange
        var parentId = new MeteringPointIdentification(1L);
        var childId = new MeteringPointIdentification(2L);
        var version = DateTimeOffset.UtcNow;

        // -- Parent metadata: MetaA(2021-01-01→2021-04-01), MetaB(2021-04-01→2021-06-01), MetaC(2021-06-01→∞)
        var parentMetadata = new List<MeteringPointMetadata>
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


        // -- Parent relations: X(03-01→05-01), Y(05-01→07-01), Z(07-01→∞)
        var parentRelations = new List<CommercialRelation>
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

        var parentMp = new MeteringPoint(
            Id: 1L,
            Version: version,
            Identification: parentId,
            MetadataTimeline: parentMetadata,
            CommercialRelationTimeline: parentRelations);

        // -- Child metadata: CM1(02-01→05-15), CM2(05-15→∞)
        var childMetadata = new List<MeteringPointMetadata>
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

        var childMp = new MeteringPoint(
            Id: 2L,
            Version: version,
            Identification: childId,
            MetadataTimeline: childMetadata,
            CommercialRelationTimeline: []);

        var hierarchy = new MeteringPointHierarchy(
            Parent: parentMp,
            ChildMeteringPoints: [childMp],
            Version: DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var buildMethod = typeof(MeasurementsReportService)
            .GetMethod("BuildMergedTimeline", BindingFlags.Static | BindingFlags.NonPublic)!;
        var segments = (List<TimelineSegment>)buildMethod.Invoke(null, new object[] { hierarchy })!;

        // Assert
        // Expect 11 slices (6 parents + 5 children)
        Assert.Equal(11, segments.Count);

        // Parent slices (TransactionType values)
        AssertSegment(segments[0], parentId, Instant.FromUtc(2021, 1, 1, 0, 0), Instant.FromUtc(2021, 3, 1, 0, 0), "MSTDATSBM", null);
        AssertSegment(segments[1], parentId, Instant.FromUtc(2021, 3, 1, 0, 0), Instant.FromUtc(2021, 4, 1, 0, 0), "MSTDATSBM", "SupplierX");
        AssertSegment(segments[2], parentId, Instant.FromUtc(2021, 4, 1, 0, 0), Instant.FromUtc(2021, 5, 1, 0, 0), "CHANGESUP", "SupplierX");
        AssertSegment(segments[3], parentId, Instant.FromUtc(2021, 5, 1, 0, 0), Instant.FromUtc(2021, 6, 1, 0, 0), "CHANGESUP", "SupplierY");
        AssertSegment(segments[4], parentId, Instant.FromUtc(2021, 6, 1, 0, 0), Instant.FromUtc(2021, 7, 1, 0, 0), "MSTDATSBM", "SupplierY");
        AssertSegment(segments[5], parentId, Instant.FromUtc(2021, 7, 1, 0, 0), Instant.MaxValue, "MSTDATSBM", "SupplierZ");

        // Child slices
        AssertSegment(segments[6], childId, Instant.FromUtc(2021, 2, 1, 0, 0), Instant.FromUtc(2021, 3, 1, 0, 0), "MOVEINES", null);
        AssertSegment(segments[7], childId, Instant.FromUtc(2021, 3, 1, 0, 0), Instant.FromUtc(2021, 5, 1, 0, 0), "MOVEINES", "SupplierX");
        AssertSegment(segments[8], childId, Instant.FromUtc(2021, 5, 1, 0, 0), Instant.FromUtc(2021, 5, 15, 0, 0), "MOVEINES", "SupplierY");
        AssertSegment(segments[9], childId, Instant.FromUtc(2021, 5, 15, 0, 0), Instant.FromUtc(2021, 7, 1, 0, 0), "MOVED", "SupplierY");
        AssertSegment(segments[10], childId, Instant.FromUtc(2021, 7, 1, 0, 0), Instant.MaxValue, "MOVED", "SupplierZ");
    }

    private static void AssertSegment(
        TimelineSegment seg,
        MeteringPointIdentification expectedId,
        Instant expectedStart,
        Instant expectedEnd,
        string expectedMetadataValue,
        string? expectedSupplier)
    {
        Assert.Equal(expectedId, seg.Identification);
        Assert.Equal(expectedStart, seg.Period.Start);
        Assert.Equal(expectedEnd, seg.Period.End);
        Assert.Equal(expectedMetadataValue, seg.Metadata.TransactionType);
        Assert.Equal(expectedSupplier, seg.Relation?.EnergySupplier);
    }


    private sealed class TestContextFactory : IDbContextFactory<ElectricityMarketDatabaseContext>
    {
        private readonly ElectricityMarketDatabaseContextFixture _fixture;

        public TestContextFactory(ElectricityMarketDatabaseContextFixture fixture)
        {
            _fixture = fixture;
        }

        public ElectricityMarketDatabaseContext CreateDbContext()
        {
            return _fixture.DatabaseManager.CreateDbContext();
        }
    }
}
