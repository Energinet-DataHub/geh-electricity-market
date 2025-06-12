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
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Common;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ConnectionState = Energinet.DataHub.ElectricityMarket.Domain.Models.ConnectionState;
using MeteringPointIdentification = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointIdentification;
using MeteringPointSubType = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointSubType;
using MeteringPointType = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointType;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Repositories;

public class MeteringPointRepositoryTests : IClassFixture<ElectricityMarketDatabaseContextFixture>, IAsyncLifetime
{
    private readonly ElectricityMarketDatabaseContextFixture _fixture;

    public MeteringPointRepositoryTests(ElectricityMarketDatabaseContextFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task GivenNetSettlementGroup6MeteringPoint_WhenQueryingSinceBeginningOfTime_ThenMeteringPointHierarchyIsReturned()
    {
        // Given metering points
        var parentVersion = DateTimeOffset.MinValue.AddYears(10);
        var childVersion = DateTimeOffset.MinValue.AddYears(40);
        var (parentIdentification, childIdentification) = await CreateTestDataAsync(parentVersion, MeteringPointType.Consumption, childVersion, MeteringPointType.Consumption, 6);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));
        var syncJob = new SyncJob(SyncJobName.CapacitySettlement, DateTimeOffset.MinValue, 0);

        // When querying
        var hierarchies = await sut.GetNetConsumptionMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Single(hierarchies);
        Assert.Equal(childVersion, hierarchies[0].Version);

        Assert.NotNull(hierarchies[0].Parent);
        Assert.Equal(parentIdentification, hierarchies[0].Parent.Identification);

        Assert.Single(hierarchies[0].ChildMeteringPoints);
        Assert.Equal(childIdentification, hierarchies[0].ChildMeteringPoints.First().Identification);
    }

    [Fact]
    public async Task GivenCapacitySettlementMeteringPoint_WhenQueryingSinceBeginningOfTime_ThenMeteringPointHierarchyIsReturned()
    {
        // Given metering points
        var parentVersion = DateTimeOffset.MinValue.AddYears(10);
        var childVersion = DateTimeOffset.MinValue.AddYears(40);
        var (parentIdentification, childIdentification) = await CreateTestDataAsync(parentVersion, MeteringPointType.Consumption, childVersion, MeteringPointType.CapacitySettlement);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));
        var syncJob = new SyncJob(SyncJobName.CapacitySettlement, DateTimeOffset.MinValue, 0);

        // When querying
        var hierarchies = await sut.GetCapacitySettlementMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Single(hierarchies);
        Assert.Equal(childVersion, hierarchies[0].Version);

        Assert.NotNull(hierarchies[0].Parent);
        Assert.Equal(parentIdentification, hierarchies[0].Parent.Identification);

        Assert.Single(hierarchies[0].ChildMeteringPoints);
        Assert.Equal(childIdentification, hierarchies[0].ChildMeteringPoints.First().Identification);
    }

    [Fact]
    public async Task GivenChangedCapacitySettlementMeteringPoint_WhenQuerying_ThenMeteringPointHierarchyIsReturned()
    {
        // Given changed child capacity settlement metering point
        var parentVersion = DateTimeOffset.MinValue.AddYears(10);
        var childVersion = DateTimeOffset.MinValue.AddYears(20);
        var (parentIdentification, childIdentification) = await CreateTestDataAsync(parentVersion, MeteringPointType.Consumption, childVersion, MeteringPointType.CapacitySettlement);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));
        var syncJob = new SyncJob(SyncJobName.CapacitySettlement, DateTimeOffset.MinValue.AddYears(15), 0);

        // When querying
        var hierarchies = await sut.GetCapacitySettlementMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Single(hierarchies);
        Assert.Equal(childVersion, hierarchies[0].Version);

        Assert.NotNull(hierarchies[0].Parent);
        Assert.Equal(parentIdentification, hierarchies[0].Parent.Identification);

        Assert.Single(hierarchies[0].ChildMeteringPoints);
        Assert.Equal(childIdentification, hierarchies[0].ChildMeteringPoints.First().Identification);
    }

    [Fact]
    public async Task GivenChangedCapacitySettlementParentMeteringPoint_WhenQuerying_ThenMeteringPointHierarchyIsReturned()
    {
        // Given changed parent metering point
        var parentVersion = DateTimeOffset.MinValue.AddYears(25);
        var childVersion = DateTimeOffset.MinValue.AddYears(10);
        var (parentIdentification, childIdentification) = await CreateTestDataAsync(parentVersion, MeteringPointType.Consumption, childVersion, MeteringPointType.CapacitySettlement);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));
        var syncJob = new SyncJob(SyncJobName.CapacitySettlement, DateTimeOffset.MinValue.AddYears(15), 0);

        // When querying
        var hierarchies = await sut.GetCapacitySettlementMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Single(hierarchies);
        Assert.Equal(parentVersion, hierarchies[0].Version);

        Assert.NotNull(hierarchies[0].Parent);
        Assert.Equal(parentIdentification, hierarchies[0].Parent.Identification);

        Assert.Single(hierarchies[0].ChildMeteringPoints);
        Assert.Equal(childIdentification, hierarchies[0].ChildMeteringPoints.First().Identification);
    }

    [Fact]
    public async Task GivenMultipleChangedCapacitySettlementMeteringPoints_WhenQuerying_ThenMeteringPointsAreReturnedOrdered()
    {
        // Given multiple changed metering points
        var parent1Version = DateTimeOffset.MinValue.AddYears(50);
        var child1Version = DateTimeOffset.MinValue.AddYears(60);
        var (parent1Identification, child1Identification) = await CreateTestDataAsync(parent1Version, MeteringPointType.Consumption, child1Version, MeteringPointType.CapacitySettlement);
        var parent2Version = DateTimeOffset.MinValue.AddYears(110);
        var child2Version = DateTimeOffset.MinValue.AddYears(120);
        var (parent2Identification, child2Identification) = await CreateTestDataAsync(parent2Version, MeteringPointType.Consumption, child2Version, MeteringPointType.CapacitySettlement);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));
        var syncJob = new SyncJob(SyncJobName.CapacitySettlement, DateTimeOffset.MinValue.AddYears(15), 0);

        // When querying
        var hierarchies = await sut.GetCapacitySettlementMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchies are returned ordered by version
        Assert.NotNull(hierarchies);
        Assert.Equal(2, hierarchies.Count);
        var hierarchy1 = hierarchies[0];
        var hierarchy2 = hierarchies[1];
        Assert.Equal(child1Version, hierarchy1.Version);
        Assert.Equal(child2Version, hierarchy2.Version);

        Assert.NotNull(hierarchy1.Parent);
        Assert.Equal(parent1Identification, hierarchy1.Parent.Identification);

        Assert.NotNull(hierarchy2.Parent);
        Assert.Equal(parent2Identification, hierarchy2.Parent.Identification);

        Assert.Single(hierarchy1.ChildMeteringPoints);
        Assert.Equal(child1Identification, hierarchy1.ChildMeteringPoints.First().Identification);

        Assert.Single(hierarchy2.ChildMeteringPoints);
        Assert.Equal(child2Identification, hierarchy2.ChildMeteringPoints.First().Identification);
    }

    [Fact]
    public async Task GivenElectricalHeatingMeteringPoint_WhenQueryingFromBeginningOfTime_ThenReturnsHierarchy()
    {
        // Given metering points
        var syncJob = new SyncJob(SyncJobName.ElectricalHeating, DateTimeOffset.MinValue, 0);
        var (parentIdentification, childIdentification) = await CreateTestDataAsync(DateTimeOffset.MinValue.AddYears(10), MeteringPointType.Consumption, DateTimeOffset.MinValue.AddYears(40), MeteringPointType.ElectricalHeating);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));

        // When querying
        var hierarchies = await sut.GetElectricalHeatingMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Single(hierarchies);
        Assert.Equal(DateTimeOffset.MinValue.AddYears(40), hierarchies[0].Version);

        Assert.NotNull(hierarchies[0].Parent);
        Assert.Equal(parentIdentification, hierarchies[0].Parent.Identification);

        Assert.Single(hierarchies[0].ChildMeteringPoints);
        Assert.Equal(childIdentification, hierarchies[0].ChildMeteringPoints.First().Identification);
    }

    [Fact]
    public async Task GivenElectricalHeatingMeteringPointWithNoChildren_WhenQueryingFromBeginningOfTime_ThenReturnsHierarchy()
    {
        // Given metering points
        var syncJob = new SyncJob(SyncJobName.ElectricalHeating, DateTimeOffset.MinValue, 0);
        var parentIdentification = await CreateTestDataNoChildrenAsync(DateTimeOffset.MinValue.AddYears(10), MeteringPointType.Consumption);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));

        // When querying
        var hierarchies = await sut.GetElectricalHeatingMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Single(hierarchies);
        Assert.Equal(DateTimeOffset.MinValue.AddYears(10), hierarchies[0].Version);

        Assert.NotNull(hierarchies[0].Parent);
        Assert.Equal(parentIdentification, hierarchies[0].Parent.Identification);

        Assert.Empty(hierarchies[0].ChildMeteringPoints);
    }

    [Fact]
    public async Task GivenElectricalHeatingMeteringPoint_WhenOnlyChildrenAreInRange_ThenReturnsHierarchy()
    {
        // Given metering points
        var syncJob = new SyncJob(SyncJobName.ElectricalHeating, DateTimeOffset.MinValue.AddYears(30), 0);
        var (parentIdentification, childIdentification) = await CreateTestDataAsync(DateTimeOffset.MinValue.AddYears(10), MeteringPointType.Consumption, DateTimeOffset.MinValue.AddYears(40), MeteringPointType.ElectricalHeating);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));

        // When querying
        var hierarchies = await sut.GetElectricalHeatingMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Single(hierarchies);
        Assert.Equal(DateTimeOffset.MinValue.AddYears(40), hierarchies[0].Version);

        Assert.NotNull(hierarchies[0].Parent);
        Assert.Equal(parentIdentification, hierarchies[0].Parent.Identification);
        Assert.Single(hierarchies[0].Parent.MetadataTimeline);

        Assert.Single(hierarchies[0].ChildMeteringPoints);
        Assert.Equal(childIdentification, hierarchies[0].ChildMeteringPoints.First().Identification);
    }

    [Fact]
    public async Task GivenElectricalHeatingMeteringPoint_WhenNothingInRange_ThenReturnsEmpty()
    {
        // Given metering points
        var syncJob = new SyncJob(SyncJobName.ElectricalHeating, DateTimeOffset.MinValue.AddYears(50), 0);
        await CreateTestDataAsync(DateTimeOffset.MinValue.AddYears(10), MeteringPointType.Consumption, DateTimeOffset.MinValue.AddYears(40), MeteringPointType.ElectricalHeating);
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointRepository(null!, dbContext, null!, new TestContextFactory(_fixture));

        // When querying
        var hierarchies = await sut.GetElectricalHeatingMeteringPointHierarchiesToSyncAsync(syncJob).ToListAsync();

        // Then hierarchy is returned
        Assert.NotNull(hierarchies);
        Assert.Empty(hierarchies);
    }

    private async Task<(MeteringPointIdentification ParentIdentification, MeteringPointIdentification ChildIdentification)> CreateTestDataAsync(DateTimeOffset parentVersion, MeteringPointType parentMeteringPointType, DateTimeOffset childVersion, MeteringPointType childMeteringPointType, int? parentSettlementGroup = null)
    {
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();

        // Parent metering point
        var parentMeteringPointEntity = CreateParentMeteringPoint(parentVersion, parentMeteringPointType, parentSettlementGroup);
        var parentIdentification = new MeteringPointIdentification(parentMeteringPointEntity.Identification);
        await dbContext.MeteringPoints.AddAsync(parentMeteringPointEntity);

        // Child metering point
        var childMeteringPointEntity = CreateChildMeteringPoint(parentIdentification.Value, childVersion, childMeteringPointType);
        var childIdentification = new MeteringPointIdentification(childMeteringPointEntity.Identification);
        await dbContext.MeteringPoints.AddAsync(childMeteringPointEntity);

        // Unrelated metering point
        var unrelatedMeteringPointEntity = CreateUnrelatedMeteringPoint();
        await dbContext.MeteringPoints.AddAsync(unrelatedMeteringPointEntity);

        await dbContext.SaveChangesAsync();
        return (parentIdentification, childIdentification);
    }

    private async Task<MeteringPointIdentification> CreateTestDataNoChildrenAsync(DateTimeOffset parentVersion, MeteringPointType parentMeteringPointType, int? parentSettlementGroup = null)
    {
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();

        // Parent metering point
        var parentMeteringPointEntity = CreateParentMeteringPoint(parentVersion, parentMeteringPointType, parentSettlementGroup);
        await dbContext.MeteringPoints.AddAsync(parentMeteringPointEntity);

        // Unrelated metering point
        var unrelatedMeteringPointEntity = CreateUnrelatedMeteringPoint();
        await dbContext.MeteringPoints.AddAsync(unrelatedMeteringPointEntity);

        await dbContext.SaveChangesAsync();
        return new MeteringPointIdentification(parentMeteringPointEntity.Identification);
    }

    private MeteringPointEntity CreateParentMeteringPoint(DateTimeOffset parentVersion, MeteringPointType parentMeteringPointType, int? parentSettlementGroup = null)
    {
        var parentIdentification = Some.MeteringPointIdentification();
        var installationAddress = Some.InstallationAddressEntity();
        var meteringPointPeriodEntity = new MeteringPointPeriodEntity()
        {
            ValidFrom = DateTimeOffset.Now.AddDays(-1),
            ValidTo = DateTimeOffset.Now.AddDays(2),
            CreatedAt = DateTimeOffset.Now,
            GridAreaCode = "001",
            OwnedBy = "4672928796219",
            ConnectionState = ConnectionState.Connected.ToString(),
            Type = parentMeteringPointType.ToString(),
            SubType = MeteringPointSubType.Physical.ToString(),
            Resolution = "PT15M",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = 1,
            MeasureUnit = MeasureUnit.kWh.ToString(),
            Product = Product.EnergyActive.ToString(),
            TransactionType = "CREATEMP",
            InstallationAddress = installationAddress,
            SettlementGroup = parentSettlementGroup,
            ParentIdentification = null
        };
        var retiredInstallationAddress = Some.InstallationAddressEntity();
        var retiredMeteringPointPeriodEntity = new MeteringPointPeriodEntity()
        {
            ValidFrom = DateTimeOffset.MinValue,
            ValidTo = meteringPointPeriodEntity.ValidFrom,
            CreatedAt = DateTimeOffset.Now,
            GridAreaCode = "001",
            OwnedBy = "4672928796219",
            ConnectionState = ConnectionState.New.ToString(),
            Type = parentMeteringPointType.ToString(),
            SubType = MeteringPointSubType.Physical.ToString(),
            Resolution = "PT15M",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = 1,
            MeasureUnit = MeasureUnit.kWh.ToString(),
            Product = Product.EnergyActive.ToString(),
            TransactionType = "CREATEMP",
            InstallationAddress = retiredInstallationAddress,
            SettlementGroup = parentSettlementGroup,
            ParentIdentification = null,
            RetiredBy = meteringPointPeriodEntity
        };
        var electricalHeatingPeriodEntity = new ElectricalHeatingPeriodEntity()
        {
            ValidFrom = DateTimeOffset.Now.AddDays(-1),
            ValidTo = DateTimeOffset.MaxValue,
            CreatedAt = DateTimeOffset.Now,
            TransactionType = "MDCNSEHON"
        };
        var commercialRelationEntity = new CommercialRelationEntity()
        {
            EndDate = DateTimeOffset.MaxValue,
            EnergySupplier = "ElSupplier A/S",
            StartDate = DateTimeOffset.Now.AddDays(-2),
            ModifiedAt = DateTimeOffset.Now,
            ElectricalHeatingPeriods = { electricalHeatingPeriodEntity }
        };
        var meteringPointEntity = new MeteringPointEntity()
        {
            Identification = parentIdentification.Value,
            Version = parentVersion,
            MeteringPointPeriods = { meteringPointPeriodEntity, retiredMeteringPointPeriodEntity },
            CommercialRelations = { commercialRelationEntity }
        };
        return meteringPointEntity;
    }

    private MeteringPointEntity CreateChildMeteringPoint(long parentIdentification, DateTimeOffset childVersion, MeteringPointType childMeteringPointType)
    {
        var childInstallationAddress = Some.InstallationAddressEntity();
        var secondChildInstallationAddress = Some.InstallationAddressEntity();
        var childMeteringPointPeriodEntity = new MeteringPointPeriodEntity()
        {
            ValidFrom = DateTimeOffset.Now.AddDays(-7),
            ValidTo = DateTimeOffset.Now.AddDays(3),
            CreatedAt = DateTimeOffset.Now,
            GridAreaCode = "002",
            OwnedBy = "4672928796220",
            ConnectionState = ConnectionState.Connected.ToString(),
            Type = MeteringPointType.Consumption.ToString(),
            SubType = MeteringPointSubType.Physical.ToString(),
            Resolution = "PT15M",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = 1,
            MeasureUnit = MeasureUnit.kWh.ToString(),
            Product = Product.EnergyActive.ToString(),
            TransactionType = "CREATEMP",
            InstallationAddress = childInstallationAddress,
            ParentIdentification = parentIdentification
        };
        var secondChildMeteringPointPeriodEntity = new MeteringPointPeriodEntity()
        {
            ValidFrom = DateTimeOffset.Now.AddDays(-7),
            ValidTo = DateTimeOffset.Now.AddDays(3),
            RetiredAt = DateTimeOffset.Now.AddHours(-1),
            RetiredBy = childMeteringPointPeriodEntity,
            CreatedAt = DateTimeOffset.Now,
            GridAreaCode = "002",
            OwnedBy = "4672928796220",
            ConnectionState = ConnectionState.Connected.ToString(),
            Type = childMeteringPointType.ToString(),
            SubType = MeteringPointSubType.Physical.ToString(),
            Resolution = "PT15M",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = 1,
            MeasureUnit = MeasureUnit.kWh.ToString(),
            Product = Product.EnergyActive.ToString(),
            TransactionType = "CREATEMP",
            InstallationAddress = secondChildInstallationAddress,
            ParentIdentification = parentIdentification
        };
        var childIdentification = Some.MeteringPointIdentification();
        var childMeteringPointEntity = new MeteringPointEntity()
        {
            Identification = childIdentification.Value,
            Version = childVersion,
            MeteringPointPeriods = { childMeteringPointPeriodEntity, secondChildMeteringPointPeriodEntity }
        };
        return childMeteringPointEntity;
    }

    private MeteringPointEntity CreateUnrelatedMeteringPoint()
    {
        var unrelatedIdentification = Some.MeteringPointIdentification();
        var unrelatedInstallationAddress = Some.InstallationAddressEntity();
        var unrelatedMeteringPointPeriodEntity = new MeteringPointPeriodEntity()
        {
            ValidFrom = DateTimeOffset.Now.AddDays(-1),
            ValidTo = DateTimeOffset.Now.AddDays(2),
            CreatedAt = DateTimeOffset.Now,
            GridAreaCode = "001",
            OwnedBy = "4672928796219",
            ConnectionState = ConnectionState.Connected.ToString(),
            Type = MeteringPointType.Consumption.ToString(),
            SubType = MeteringPointSubType.Physical.ToString(),
            Resolution = "PT15M",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = 1,
            MeasureUnit = MeasureUnit.kWh.ToString(),
            Product = Product.EnergyActive.ToString(),
            TransactionType = "CREATEMP",
            InstallationAddress = unrelatedInstallationAddress
        };
        var unrelatedMeteringPointEntity = new MeteringPointEntity()
        {
            Identification = unrelatedIdentification.Value,
            Version = DateTimeOffset.MinValue.AddYears(30),
            MeteringPointPeriods = { unrelatedMeteringPointPeriodEntity }
        };
        return unrelatedMeteringPointEntity;
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
