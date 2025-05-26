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
using Energinet.DataHub.ElectricityMarket.Application.Handlers;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
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

public class VerifyGridOwnerRepositoryTests : IClassFixture<ElectricityMarketDatabaseContextFixture>, IAsyncLifetime
{
    private readonly ElectricityMarketDatabaseContextFixture _fixture;

    public VerifyGridOwnerRepositoryTests(ElectricityMarketDatabaseContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenMeteringPoints_WhenQueryingForParentHierarchy_ThenCompleteHierarchyIsReturned()
    {
        var parentIdentification = await CreateTestDataAsync();

        var dbContext = _fixture.DatabaseManager.CreateDbContext();
        await using var electricityMarketDatabaseContext = dbContext;

        var sut = new MeteringPointRepository(null!, dbContext, null!, null!);

        var res = await sut.GetMeteringPointForSignatureAsync(parentIdentification);

        // var verifyGridOwner = new VerifyGridOwnerHandler()
        Assert.NotNull(res);
        Assert.NotNull(res.Identification);
        Assert.Equal(parentIdentification, res.Identification);
        Assert.Equal("001", res.Metadata.GridAreaCode);
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private async Task<MeteringPointIdentification> CreateTestDataAsync()
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
        return parentIdentification;
    }
}
