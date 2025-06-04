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

    [Fact]
    public void GivenCapacitySettlementHierarchy_WhenFindingCapacitySettlementPeriods_ReturnPeriod()
    {
        // Given capacity settlement hierarchy
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(ValidIntervalNow(), Any.MeteringPointIdentification());
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata], DateTimeOffset.Now);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, parentMeteringPointMetadata, DateTimeOffset.Now.AddDays(-2), []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // Single capacity settlement period found
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.Single();
        Assert.Equal(parentMeteringPoint.Identification.Value, dto.MeteringPointId);
        Assert.Equal(parentMeteringPointMetadata.Valid.Start.ToDateTimeOffset(), dto.PeriodFromDate);
        Assert.Equal(parentMeteringPointMetadata.Valid.End.ToDateTimeOffset(), dto.PeriodToDate);
        Assert.Equal(capacitySettlementMeteringPoint.Identification.Value, dto.ChildMeteringPointId);
        Assert.Equal(capacitySettlementMeteringPointMetadata.Valid.Start.ToDateTimeOffset(), dto.ChildPeriodFromDate);
        Assert.Null(dto.ChildPeriodToDate);
    }

    [Fact]
    public void GivenCapacitySettlementChildClosedDownBefore2025_WhenFindingCapacitySettlementPeriods_NoPeriodsAreReturned()
    {
        // Given closed down capacity settlement hierarchy
        var parentIdentification = Any.MeteringPointIdentification();
        var interval = new Interval(Instant.FromUtc(2024, 1, 1, 0, 0, 0), Instant.FromUtc(2024, 12, 31, 22, 59, 59));
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(interval, parentIdentification);
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, null), parentIdentification, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed], DateTimeOffset.Now);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, parentMeteringPointMetadata, DateTimeOffset.Now, []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // No capacity settlement period found
        Assert.Empty(capacitySettlementPeriods);
    }

    [Fact]
    public void GivenCapacityChildMeteringPointBefore2025_WhenFindingCapacitySettlementPeriods_ReturnEmptyPeriod()
    {
        // Given capacity settlement period From 2024
        var interval = new Interval(Instant.FromUtc(2024, 1, 1, 0, 0, 0), Instant.FromUtc(2024, 12, 31, 22, 59, 59));
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(interval, Any.MeteringPointIdentification());
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata], DateTimeOffset.Now);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, parentMeteringPointMetadata, DateTimeOffset.Now.AddDays(-2), []);

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
        var capacitySettlementStart = Instant.FromUtc(2024, 12, 25, 0, 0, 0);
        var capacitySettlementEnd = Instant.FromUtc(2025, 2, 23, 12, 0, 0);
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementStart,  capacitySettlementEnd), Any.MeteringPointIdentification());
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, capacitySettlementMeteringPointMetadata.Valid.End.Plus(Duration.FromDays(1))), null, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed], DateTimeOffset.Now);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, parentMeteringPointMetadata, DateTimeOffset.Now.AddDays(-2), []);

        var hierarchy = new MeteringPointHierarchy(parentMeteringPoint, [capacitySettlementMeteringPoint], DateTimeOffset.MinValue);

        // When finding capacity settlement periods to sync
        var capacitySettlementPeriods = _sut.GetCapacitySettlementPeriods(hierarchy).ToList();

        // Capacity settlement period starts 1/1-2025
        Assert.Single(capacitySettlementPeriods);
        var dto = capacitySettlementPeriods.Single();
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(1)), dto.ChildPeriodFromDate);
        Assert.Equal(new DateTimeOffset(2025, 2, 23, 13, 0, 0, TimeSpan.FromHours(1)), dto.ChildPeriodToDate);
    }

    [Fact]
    public void GivenMultipleCommercialRelations_WhenFindingCapacitySettlementPeriods_ReturnPeriodPerCommercialRelation()
    {
        // Given multiple commercial relations
        var capacitySettlementStart = Instant.FromUtc(2024, 12, 25, 0, 0, 0);
        var capacitySettlementEnd = Instant.FromUtc(2025, 2, 23, 0, 0, 0);
        var capacitySettlementMeteringPointMetadata = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementStart,  capacitySettlementEnd), Any.MeteringPointIdentification());
        var capacitySettlementMeteringPointMetadataClosed = CreateCapacitySettlementMeteringPointMetadata(new Interval(capacitySettlementMeteringPointMetadata.Valid.End, capacitySettlementMeteringPointMetadata.Valid.End.Plus(Duration.FromDays(1))), null, ConnectionState.ClosedDown);
        var capacitySettlementMeteringPoint = CreateCapacitySettlementChildMeteringPoint([capacitySettlementMeteringPointMetadata, capacitySettlementMeteringPointMetadataClosed], DateTimeOffset.Now);

        var parentMeteringPointMetadata = CreateParentMeteringPointMetadata(ValidIntervalNow());
        var commercialRelation1Interval = new Interval(Instant.FromUtc(2024, 12, 25, 0, 0, 0), Instant.FromUtc(2025, 2, 21, 0, 0, 0));
        var commercialRelation1 = new CommercialRelation(1, "Watts Inc.", commercialRelation1Interval, Guid.NewGuid(), [], []);
        var commercialRelation2Interval = new Interval(Instant.FromUtc(2025, 2, 22, 0, 0, 0), Instant.FromUtc(2025, 3, 19, 0, 0, 0));
        var commercialRelation2 = new CommercialRelation(1, "Watts Inc.", commercialRelation2Interval, Guid.NewGuid(), [], []);
        var commercialRelation3Interval = new Interval(Instant.FromUtc(2024, 2, 22, 0, 0, 0), Instant.FromUtc(2024, 3, 19, 0, 0, 0));
        var commercialRelation3 = new CommercialRelation(1, "Watts Inc.", commercialRelation3Interval, Guid.NewGuid(), [], []);
        var parentMeteringPoint = CreateParentMeteringPoint(capacitySettlementMeteringPointMetadata.Parent!, parentMeteringPointMetadata, DateTimeOffset.Now.AddDays(-2), [commercialRelation1, commercialRelation2, commercialRelation3]);

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
        Assert.Equal(commercialRelation2.Period.End.ToDateTimeOffset(), dto2.PeriodToDate);
    }

    private static MeteringPoint CreateParentMeteringPoint(MeteringPointIdentification parentIdentification, MeteringPointMetadata meteringPointMetadata, DateTimeOffset version, List<CommercialRelation> commercialRelations)
    {
        return new MeteringPoint(1, version, parentIdentification, new List<MeteringPointMetadata>() { meteringPointMetadata }, commercialRelations);
    }

    private static MeteringPointMetadata CreateParentMeteringPointMetadata(Interval valid)
    {
        return new MeteringPointMetadata(
            2,
            valid,
            null,
            MeteringPointType.Consumption,
            MeteringPointSubType.Physical,
            ConnectionState.Connected,
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

    private static MeteringPoint CreateCapacitySettlementChildMeteringPoint(List<MeteringPointMetadata> meteringPointMetadataList, DateTimeOffset version)
    {
        return new MeteringPoint(1, version, Any.MeteringPointIdentification(), meteringPointMetadataList, new List<CommercialRelation>());
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
}
