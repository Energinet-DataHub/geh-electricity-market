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
using Energinet.DataHub.ElectricityMarket.Application.Common;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.UnitTests.Common;
using NodaTime;
using Xunit;
using SystemClock = NodaTime.SystemClock;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Services;

public class CapacitySettlementServiceTests
{
    private readonly CapacitySettlementService _sut = new();
    private readonly Instant _systemTime = SystemClock.Instance.GetCurrentInstant();
    private readonly Instant _date29112024 = Instant.FromUtc(2024, 11, 29, 22, 59, 59);
    private readonly Instant _date29092026 = Instant.FromUtc(2026, 9, 29, 22, 59, 59);
    private readonly Instant _dateStartOf2025 = Instant.FromUtc(2024, 12, 31, 23, 0, 0);
    private readonly Instant _date25122024 = Instant.FromUtc(2024, 12, 25, 0, 0, 0);
    private readonly Instant _date23022025 = Instant.FromUtc(2025, 2, 23, 0, 0, 0);

    [Fact]
    public void GivenPlainCapacitySettlementHierarchy_WhenFindingCapacitySettlementPeriods_ReturnPeriod()
    {
        // Given capacity settlement hierarchy
        var interval = ValidIntervalNow();
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(interval, Any.MeteringPointIdentification());
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata], []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // Single capacity settlement period found
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.Single();
        Assert.Equal(parentMeteringPoint.Identification.Value, dto.MeteringPointId);
        Assert.Equal(parentMeteringPointMetadata.Valid.Start.ToDateTimeOffset(), dto.PeriodFromDate);
        Assert.Equal(DateTimeOffset.MaxValue, dto.PeriodToDate);
        Assert.Equal(capacitySettlementMeteringPoint.Identification.Value, dto.ChildMeteringPointId);
        Assert.Equal(interval.Start.ToDateTimeOffset(), dto.ChildPeriodFromDate);
        Assert.Equal(interval.End.ToDateTimeOffset(), dto.ChildPeriodToDate);
    }

    [Fact]
    public void GivenCapacitySettlementChildClosedDownBefore2025_WhenFindingCapacitySettlementPeriods_NoPeriodsAreReturned()
    {
        // Given closed down capacity settlement hierarchy
        var parentIdentification = Any.MeteringPointIdentification();
        var interval = new Interval(AnyDateInYear(2023), AnyDateInYear(2024));
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(interval, parentIdentification);
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(interval.End, null), parentIdentification, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata], []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // No capacity settlement period found
        Assert.Empty(capacitySettlementPeriods);
    }

    [Fact]
    public void GivenCapacitySettlementChildClosedDownAfter2025_WhenFindingCapacitySettlementPeriods_PeriodsAreReturned()
    {
        // Given closed down capacity settlement hierarchy
        var parentIdentification = Any.MeteringPointIdentification();
        var interval = new Interval(AnyDateInYear(2024), AnyDateInYear(2026));
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(interval, parentIdentification);
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(interval.End, null), parentIdentification, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata], []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // Single capacity settlement period found
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.Single();
        Assert.Equal(parentMeteringPoint.Identification.Value, dto.MeteringPointId);
        Assert.Equal(parentMeteringPointMetadata.Valid.Start.ToDateTimeOffset(), dto.PeriodFromDate);
        Assert.Equal(DateTimeOffset.MaxValue, dto.PeriodToDate);
        Assert.Equal(capacitySettlementMeteringPoint.Identification.Value, dto.ChildMeteringPointId);
        Assert.Equal(_dateStartOf2025.ToDateTimeOffset(), dto.ChildPeriodFromDate);
        Assert.Equal(interval.End.ToDateTimeOffset(), dto.ChildPeriodToDate);
    }

    [Fact]
    public void GivenCapacityChildMeteringPointBefore2025_WhenFindingCapacitySettlementPeriods_ReturnEmptyPeriod()
    {
        // Given capacity settlement period From 2024
        var interval = new Interval(AnyDateInYear(2022), AnyDateInYear(2024));
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(interval, Any.MeteringPointIdentification());
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata], []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy);

        // No capacity settlement period found
        Assert.Empty(capacitySettlementPeriods);
    }

    [Fact]
    public void GivenCapacitySettlementAttachedBefore2025_WhenFindingCapacitySettlementPeriods_ReturnedPeriodStartsIn2025()
    {
        // Given capacity settlement period before 2025
        var capacitySettlementStart = _date29112024;
        var capacitySettlementEnd = _date29092026;
        var parentIdentification = Any.MeteringPointIdentification();
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementStart,  capacitySettlementEnd), parentIdentification);
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, capacitySettlementMeteringPointMetadata.Valid.End.Plus(Duration.FromDays(1))), parentIdentification, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata], []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // Capacity settlement period starts 1/1-2025
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.Single();
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(1)), dto.ChildPeriodFromDate);
        Assert.Equal(_date29092026.ToDateTimeOffset(), dto.ChildPeriodToDate);
    }

    [Fact]
    public void GivenReAttachedCapacitySettlementMeteringPoint_WhenFindingCapacitySettlementPeriods_ReturnTwoPeriods()
    {
        // Given multiple capacity settlement metering point periods
        var interval1 = new Interval(AnyDateInYear(2026), AnyDateInYear(2028));
        var interval2 = new Interval(AnyDateInYear(2031), AnyDateInYear(2033));
        var parenIdentification = Any.MeteringPointIdentification();
        var capacitySettlementMeteringPointMetadata1 = CreateCapacitySettlementMeteringPointMetadata(interval1, parenIdentification);
        var capacitySettlementMeteringPointMetadata2 = CreateCapacitySettlementMeteringPointMetadata(interval2, parenIdentification);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata1, capacitySettlementMeteringPointMetadata2]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(new Interval(interval1.Start, interval2.End));
        var parentMeteringPoint = CreateParentMeteringPoint(parenIdentification, [parentMeteringPointMetadata], []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // One row per period
        Assert.Equal(2, capacitySettlementPeriods.Count);
        var dto1 = capacitySettlementPeriods.First();
        var dto2 = capacitySettlementPeriods.Last();
        Assert.Equal(interval1.Start.ToDateTimeOffset(), dto1.ChildPeriodFromDate);
        Assert.Equal(interval1.End.ToDateTimeOffset(), dto1.ChildPeriodToDate);
        Assert.Equal(interval2.Start.ToDateTimeOffset(), dto2.ChildPeriodFromDate);
        Assert.Equal(interval2.End.ToDateTimeOffset(), dto2.ChildPeriodToDate);
    }

    [Fact]
    public void GivenMultipleCapacitySettlementMeteringPoints_WhenFindingCapacitySettlementPeriods_ReturnTwoPeriods()
    {
        // Given multiple capacity settlement metering points
        var interval1 = new Interval(AnyDateInYear(2026), AnyDateInYear(2028));
        var interval2 = new Interval(AnyDateInYear(2031), AnyDateInYear(2033));
        var parentIdentification = Any.MeteringPointIdentification();
        var capacitySettlementMeteringPointMetadata1 = CreateCapacitySettlementMeteringPointMetadata(interval1, parentIdentification);
        var capacitySettlementMeteringPointMetadata2 = CreateCapacitySettlementMeteringPointMetadata(interval2, parentIdentification);
        var capacitySettlementMeteringPoint1 = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata1]);
        var capacitySettlementMeteringPoint2 = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata2]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(new Interval(interval1.Start, interval2.End));
        var parentMeteringPoint = CreateParentMeteringPoint(parentIdentification, [parentMeteringPointMetadata], []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint1, capacitySettlementMeteringPoint2], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // One row per capacity settlement metering point
        Assert.Equal(2, capacitySettlementPeriods.Count);
        var dto1 = capacitySettlementPeriods.First();
        var dto2 = capacitySettlementPeriods.Last();
        Assert.Equal(interval1.Start.ToDateTimeOffset(), dto1.ChildPeriodFromDate);
        Assert.Equal(interval1.End.ToDateTimeOffset(), dto1.ChildPeriodToDate);
        Assert.Equal(interval2.Start.ToDateTimeOffset(), dto2.ChildPeriodFromDate);
        Assert.Equal(interval2.End.ToDateTimeOffset(), dto2.ChildPeriodToDate);
    }

    [Fact]
    public void GivenMultipleCommercialRelations_WhenFindingCapacitySettlementPeriods_ReturnPeriodPerCommercialRelation()
    {
        // Given multiple commercial relations
        var capacitySettlementStart = _date25122024;
        var capacitySettlementEnd = _date23022025;
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementStart,  capacitySettlementEnd), Any.MeteringPointIdentification());
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, capacitySettlementMeteringPointMetadata.Valid.End.Plus(Duration.FromDays(1))), null, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var commercialRelation1Interval = new Interval(_date25122024, Instant.FromUtc(2025, 2, 21, 0, 0, 0));
        var commercialRelation1 = new CommercialRelation(1, "Watts Inc.", commercialRelation1Interval, Guid.NewGuid(), [], []);
        var commercialRelation2Interval = new Interval(Instant.FromUtc(2025, 2, 21, 0, 0, 0), Instant.FromUtc(2025, 3, 19, 0, 0, 0));
        var commercialRelation2 = new CommercialRelation(2, "Watts Inc.", commercialRelation2Interval, Guid.NewGuid(), [], []);
        var commercialRelation3Interval = new Interval(Instant.FromUtc(2024, 2, 22, 0, 0, 0), Instant.FromUtc(2024, 3, 19, 0, 0, 0));
        var commercialRelation3 = new CommercialRelation(3, "Watts Inc.", commercialRelation3Interval, Guid.NewGuid(), [], []);
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata], [commercialRelation1, commercialRelation2, commercialRelation3]);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // One row per commercial relation is exported
        Assert.Equal(2, capacitySettlementPeriods.Count);
        var dto1 = capacitySettlementPeriods.First();
        var dto2 = capacitySettlementPeriods.Last();
        Assert.Equal(commercialRelation1.Period.Start.ToDateTimeOffset(), dto1.PeriodFromDate);
        Assert.Equal(commercialRelation1.Period.End.ToDateTimeOffset(), dto1.PeriodToDate);
        Assert.Equal(commercialRelation2.Period.Start.ToDateTimeOffset(), dto2.PeriodFromDate);
        Assert.Equal(DateTimeOffset.MaxValue, dto2.PeriodToDate);
    }

    [Fact]
    public void GivenEndedSupply_WhenFindingCapacitySettlementPeriods_ReturnedPeriodEndsAtClosedDate()
    {
        // Given ended commercial relation and open-ended consumption metering point
        var capacitySettlementStart = _date25122024;
        var capacitySettlementEnd = _date23022025;
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementStart,  capacitySettlementEnd), Any.MeteringPointIdentification());
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, capacitySettlementMeteringPointMetadata.Valid.End.Plus(Duration.FromDays(1))), null, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed]);

        var connectedInterval = new Interval(capacitySettlementStart.Plus(Duration.FromDays(10)), capacitySettlementEnd.Plus(Duration.FromDays(10)));
        var closedDownInterval = new Interval(connectedInterval.End, null);
        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(connectedInterval);
        var parentMeteringPointMetadataClosedDown = CreateParentMeteringPointMetadata(closedDownInterval, ConnectionState.ClosedDown);
        var commercialRelationInterval = new Interval(_date25122024, _date23022025);
        var commercialRelation = new CommercialRelation(1, "Watts Inc.", commercialRelationInterval, Guid.NewGuid(), [], []);
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata, parentMeteringPointMetadataClosedDown], [commercialRelation]);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // One open-ended row is returned
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.First();
        Assert.Equal(_date25122024.ToDateTimeOffset(), dto.PeriodFromDate);
        Assert.Equal(closedDownInterval.Start.ToDateTimeOffset(), dto.PeriodToDate);
    }

    [Fact]
    public void GivenMoveOut_WhenFindingCapacitySettlementPeriods_ReturnedPeriodEndsAtClosedDate()
    {
        // Given ended commercial relation and open-ended consumption metering point
        var capacitySettlementStart = _date25122024;
        var capacitySettlementEnd = _date23022025;
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementStart,  capacitySettlementEnd), Any.MeteringPointIdentification());
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, capacitySettlementMeteringPointMetadata.Valid.End.Plus(Duration.FromDays(1))), null, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed]);

        var connectedInterval = new Interval(capacitySettlementStart.Plus(Duration.FromDays(10)), capacitySettlementEnd.Plus(Duration.FromDays(10)));
        var closedDownInterval = new Interval(connectedInterval.End, null);
        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(connectedInterval);
        var parentMeteringPointMetadataClosedDown = CreateParentMeteringPointMetadata(closedDownInterval, ConnectionState.ClosedDown);
        var clientInterval = new Interval(capacitySettlementStart.Minus(Duration.FromDays(10)), capacitySettlementStart.Plus(Duration.FromDays(10)));
        var noClientInterval = new Interval(clientInterval.End, capacitySettlementEnd.Plus(Duration.FromDays(10)));
        var commercialRelation = new CommercialRelation(1, "Watts Inc.", clientInterval, Guid.NewGuid(), [], []);
        var moveOutCommercialRelation = new CommercialRelation(2, "Watts Inc.", noClientInterval, null, [], []);
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata, parentMeteringPointMetadataClosedDown], [commercialRelation, moveOutCommercialRelation]);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // One open-ended row is returned
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.First();
        Assert.Equal(clientInterval.Start.ToDateTimeOffset(), dto.PeriodFromDate);
        Assert.Equal(noClientInterval.End.ToDateTimeOffset(), dto.PeriodToDate);
    }

    [Fact]
    public void GivenCollapsedCommercialRelation_WhenFindingCapacitySettlementPeriods_CollapsedCommercialRelationIsNotIncluded()
    {
        // Given collapsed commercial relations
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(new Interval(_date25122024,  _date23022025), Any.MeteringPointIdentification());
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, capacitySettlementMeteringPointMetadata.Valid.End.Plus(Duration.FromDays(1))), null, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed]);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var clientId = Guid.NewGuid();
        var commercialRelation1Interval = new Interval(_date25122024, Instant.FromUtc(2025, 2, 21, 0, 0, 0));
        var commercialRelation1 = new CommercialRelation(1, "Watts Inc.", commercialRelation1Interval, clientId, [], []);
        var collapsedCommercialRelation2Interval = new Interval(commercialRelation1Interval.End, commercialRelation1Interval.End);
        var collapsedCommercialRelation2 = new CommercialRelation(2, "Watts Inc.", collapsedCommercialRelation2Interval, Guid.NewGuid(), [], []);
        var commercialRelation3Interval = new Interval(commercialRelation1Interval.End, Instant.MaxValue);
        var commercialRelation3 = new CommercialRelation(2, "Watts Inc.", commercialRelation3Interval, clientId, [], []);
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, [parentMeteringPointMetadata], [commercialRelation1, collapsedCommercialRelation2, commercialRelation3]);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // One row per commercial relation is exported
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.First();
        Assert.Equal(commercialRelation1.Period.Start.ToDateTimeOffset(), dto.PeriodFromDate);
        Assert.Equal(commercialRelation3.Period.End.ToDateTimeOffset(), dto.PeriodToDate);
    }

    private static MeteringPoint CreateParentMeteringPoint(MeteringPointIdentification parentIdentification, List<MeteringPointMetadata> meteringPointMetadata, List<CommercialRelation> commercialRelations)
    {
        return new MeteringPoint(1, DateTimeOffset.Now, parentIdentification, meteringPointMetadata, commercialRelations);
    }

    private static MeteringPointMetadata CreateParentMeteringPointMetadata(Interval valid, ConnectionState connectionState = ConnectionState.Connected)
    {
        return new MeteringPointMetadata(
            2,
            valid,
            null,
            MeteringPointType.Consumption,
            MeteringPointSubType.Physical,
            connectionState,
            "Resolution",
            "GridAreaCode",
            "owner",
            ConnectionType.Direct,
            DisconnectionType.ManualDisconnection,
            Product.EnergyActive,
            false,
            MeteringPointMeasureUnit.KW,
            AssetType.NoTechnology,
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
            Any.InstallationAddress(),
            TransactionTypes.MoveIn);
    }

    private static MeteringPoint CreateCapacitySettlementChildMeteringPoint(List<MeteringPointMetadata> meteringPointMetadataList)
    {
        return new MeteringPoint(1, DateTimeOffset.Now, Any.MeteringPointIdentification(), meteringPointMetadataList, new List<CommercialRelation>());
    }

    private static MeteringPointMetadata CreateCapacitySettlementMeteringPointMetadata(Interval valid, MeteringPointIdentification? parent, ConnectionState connectionState = ConnectionState.Connected)
    {
        return new MeteringPointMetadata(
            2,
            valid,
            parent,
            MeteringPointType.CapacitySettlement,
            MeteringPointSubType.Physical,
            connectionState,
            "Resolution",
            "GridAreaCode",
            "owner",
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
            Any.InstallationAddress(),
            TransactionTypes.MasterDataSent);
    }

    private Interval ValidIntervalNow()
    {
        return new Interval(_systemTime.Minus(Duration.FromDays(1)), _systemTime.Plus(Duration.FromDays(1)));
    }

    private Instant AnyDateInYear(int year)
    {
        return Instant.FromUtc(year, 5, 26, 0, 0, 0);
    }
}
