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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Common;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
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

    [Fact]
    public async Task GivenMeteringPoints_WhenQueryingForParentHierarchy_ThenCompleteHierarchyIsReturned()
    {
        var (parentIdentification, childIdentification) = await CreateTestDataAsync();

        var dbContext = _fixture.DatabaseManager.CreateDbContext();
        await using var electricityMarketDatabaseContext = dbContext;

        var sut = new MeteringPointRepository(null!, null!, dbContext, null!, null!);

        var res = await sut.GetMeteringPointHierarchyAsync(parentIdentification);

        Assert.NotNull(res);
        Assert.NotNull(res.Parent);
        Assert.Equal(parentIdentification, res.Parent.Identification);
        Assert.Single(res.Parent.MetadataTimeline);
        Assert.Single(res.ChildMeteringPoints);
        Assert.Equal(childIdentification, res.ChildMeteringPoints.First().Identification);
        Assert.Single(res.ChildMeteringPoints.First().MetadataTimeline);
    }

    [Fact]
    public async Task GivenMeteringPoints_WhenQueryingForChildHierarchy_ThenCompleteHierarchyIsReturned()
    {
        var (parentIdentification, childIdentification) = await CreateTestDataAsync();

        var dbContext = _fixture.DatabaseManager.CreateDbContext();
        await using var electricityMarketDatabaseContext = dbContext;

        var sut = new MeteringPointRepository(null!, null!, dbContext, null!, null!);

        var res = await sut.GetMeteringPointHierarchyAsync(childIdentification);

        Assert.NotNull(res);
        Assert.NotNull(res.Parent);
        Assert.Equal(parentIdentification, res.Parent.Identification);
        Assert.Single(res.Parent.MetadataTimeline);
        Assert.Single(res.ChildMeteringPoints);
        Assert.Equal(childIdentification, res.ChildMeteringPoints.First().Identification);
        Assert.Single(res.ChildMeteringPoints.First().MetadataTimeline);
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private async Task<(MeteringPointIdentification ParentIdentification, MeteringPointIdentification ChildIdentification)> CreateTestDataAsync()
    {
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext();

        // Parent metering point
        var parentIdentification = Some.MeteringPointIdentification();
        var installationAddress = Some.InstallationAddressEntity();
        await dbContext.InstallationAddresses.AddAsync(installationAddress);
        var meteringPointPeriodEntity = new MeteringPointPeriodEntity()
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
            InstallationAddress = installationAddress
        };
        await dbContext.MeteringPointPeriods.AddAsync(meteringPointPeriodEntity);
        await dbContext.MeteringPoints.AddAsync(new MeteringPointEntity()
        {
            Identification = parentIdentification.Value, MeteringPointPeriods = { meteringPointPeriodEntity }
        });

        // Child metering point
        var childInstallationAddress = Some.InstallationAddressEntity();
        var secondChildInstallationAddress = Some.InstallationAddressEntity();
        await dbContext.InstallationAddresses.AddAsync(childInstallationAddress);
        await dbContext.InstallationAddresses.AddAsync(secondChildInstallationAddress);
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
            ParentIdentification = parentIdentification.Value
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
            Type = MeteringPointType.Consumption.ToString(),
            SubType = MeteringPointSubType.Physical.ToString(),
            Resolution = "PT15M",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = 1,
            MeasureUnit = MeasureUnit.kWh.ToString(),
            Product = Product.EnergyActive.ToString(),
            TransactionType = "CREATEMP",
            InstallationAddress = secondChildInstallationAddress,
            ParentIdentification = null
        };
        var childIdentification = Some.MeteringPointIdentification();
        await dbContext.MeteringPointPeriods.AddAsync(childMeteringPointPeriodEntity);
        await dbContext.MeteringPointPeriods.AddAsync(secondChildMeteringPointPeriodEntity);
        await dbContext.MeteringPoints.AddAsync(new MeteringPointEntity()
        {
            Identification = childIdentification.Value, MeteringPointPeriods = { childMeteringPointPeriodEntity, secondChildMeteringPointPeriodEntity }
        });

        // Unrelated metering point
        var unrelatedIdentification = Some.MeteringPointIdentification();
        var unrelatedInstallationAddress = Some.InstallationAddressEntity();
        await dbContext.InstallationAddresses.AddAsync(unrelatedInstallationAddress);
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
        await dbContext.MeteringPointPeriods.AddAsync(unrelatedMeteringPointPeriodEntity);
        await dbContext.MeteringPoints.AddAsync(new MeteringPointEntity()
        {
            Identification = unrelatedIdentification.Value, MeteringPointPeriods = { unrelatedMeteringPointPeriodEntity }
        });

        await dbContext.SaveChangesAsync();
        return (parentIdentification, childIdentification);
    }
}
