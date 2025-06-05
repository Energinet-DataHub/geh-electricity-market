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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Authorize;
using Energinet.DataHub.ElectricityMarket.Application.Handlers;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Common;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using NodaTime;
using Xunit;
using ConnectionState = Energinet.DataHub.ElectricityMarket.Domain.Models.ConnectionState;
using MeteringPointIdentification = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointIdentification;
using MeteringPointSubType = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointSubType;
using MeteringPointType = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointType;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Repositories
{
    public class GetSupplierPeriodsTests : IClassFixture<ElectricityMarketDatabaseContextFixture>, IAsyncLifetime
    {
        private readonly ElectricityMarketDatabaseContextFixture _fixture;
        private readonly int _startDateOffset1 = -90;
        private readonly int _endDateOffset1 = -30;
        private readonly string _balanceSupplier1 = "12345678";
        private readonly int _startDateOffset2 = -10;
        private readonly int _startDateOffset3 = -200;
        private readonly int _endDateOffset3 = -150;
        private readonly Instant _today = Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime());
        private readonly DateTimeOffset _todayDateTime = DateTime.Today.ToUniversalTime();

        public GetSupplierPeriodsTests(ElectricityMarketDatabaseContextFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetSupplierPeriodsTest()
        {
            // arrange
            var parentIdentification = await CreateTestDataAsync();

            var dbContext = _fixture.DatabaseManager.CreateDbContext();
            await using var electricityMarketDatabaseContext = dbContext;

            var sut = new MeteringPointRepository(null!, dbContext, null!, null!);
            var res = await sut.GetMeteringPointForSignatureAsync(parentIdentification);

            Assert.NotNull(res);

            // act + assert 2 periods from 3 should be returned.
            var target = new GetSupplierPeriodsHandler(sut);

            var requestPeriod = new Interval(Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-100)), Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-1)));
            var command = new GetSupplierPeriodsCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), _balanceSupplier1, requestPeriod);
            var response = await target.Handle(command, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(2, response.Count());

            // act + assert 3 periods from 3 should be returned.
            requestPeriod = new Interval(Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-400)), Instant.FromDateTimeUtc(DateTime.UtcNow));
            command = new GetSupplierPeriodsCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), _balanceSupplier1, requestPeriod);
            response = await target.Handle(command, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(3, response.Count());

            // act + assert 1 period from 3 should be returned.
            // This period should match requested end date (=-5 from today). No end date (max value) in commercial relation.
            // Start date should match commercial relation start date, which is more recent than requested start date
            requestPeriod = new Interval(_today.Minus(Duration.FromDays(15)), _today.Minus(Duration.FromDays(5)));
            command = new GetSupplierPeriodsCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), _balanceSupplier1, requestPeriod);
            response = await target.Handle(command, CancellationToken.None);
            Assert.NotNull(response);
            Assert.Single(response);

            Assert.Equal(_today.Minus(Duration.FromDays(10)), response.First().Start);
            Assert.Equal(_today.Minus(Duration.FromDays(5)), response.First().End);

            // act + assert 1 period from 3 should be returned.
            // This period should match requested end date (=-5 from today). No end date (max value) in commercial relation.
            // Start date should match the requested start date because the commercial relation start date is earlier.
            requestPeriod = new Interval(_today.Minus(Duration.FromDays(8)), _today.Minus(Duration.FromDays(5)));
            command = new GetSupplierPeriodsCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), _balanceSupplier1, requestPeriod);
            response = await target.Handle(command, CancellationToken.None);
            Assert.NotNull(response);
            Assert.Single(response);

            Assert.Equal(_today.Minus(Duration.FromDays(8)), response.First().Start);
            Assert.Equal(_today.Minus(Duration.FromDays(5)), response.First().End);

            // act + assert no results because incorrect balance supplier.
            requestPeriod = new Interval(_today.Minus(Duration.FromDays(15)), _today.Minus(Duration.FromDays(5)));
            command = new GetSupplierPeriodsCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), "00000001", requestPeriod);
            response = await target.Handle(command, CancellationToken.None);
            Assert.Empty(response);
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
            var commercialRelationEntity1 = new CommercialRelationEntity()
            {
                StartDate = _todayDateTime.AddDays(_startDateOffset1),
                EndDate = _todayDateTime.AddDays(_endDateOffset1),
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value
            };

            // No endate
            var commercialRelationEntity2 = new CommercialRelationEntity()
            {
                StartDate = _todayDateTime.AddDays(_startDateOffset2),
                EndDate = DateTimeOffset.MaxValue,
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value
            };
            var commercialRelationEntity3 = new CommercialRelationEntity()
            {
                StartDate = _todayDateTime.AddDays(_startDateOffset3),
                EndDate = _todayDateTime.AddDays(_endDateOffset3),
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value
            };
            await dbContext.MeteringPointPeriods.AddAsync(meteringPointPeriodEntity);
            await dbContext.CommercialRelations.AddAsync(commercialRelationEntity1);
            await dbContext.CommercialRelations.AddAsync(commercialRelationEntity2);
            await dbContext.CommercialRelations.AddAsync(commercialRelationEntity3);
            await dbContext.MeteringPoints.AddAsync(new MeteringPointEntity()
            {
                Identification = parentIdentification.Value,
                MeteringPointPeriods = { meteringPointPeriodEntity },
                CommercialRelations = { commercialRelationEntity1, commercialRelationEntity2, commercialRelationEntity3 }
            });

            await dbContext.SaveChangesAsync();
            return parentIdentification;
        }
    }
}
