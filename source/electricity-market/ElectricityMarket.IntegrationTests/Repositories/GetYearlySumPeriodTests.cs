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
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using Xunit;
using ConnectionState = Energinet.DataHub.ElectricityMarket.Domain.Models.ConnectionState;
using MeteringPointIdentification = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointIdentification;
using MeteringPointSubType = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointSubType;
using MeteringPointType = Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointType;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Repositories
{
    public class GetYearlySumPeriodTests : IClassFixture<ElectricityMarketDatabaseContextFixture>, IAsyncLifetime
    {
        private readonly ElectricityMarketDatabaseContextFixture _fixture;
        private readonly string _balanceSupplier1 = "12345678";
        private readonly Instant _yearAgo = Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime().AddDays(-365));
        private readonly Instant _today = Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime());
        private readonly DateTimeOffset _todayDateTime = DateTime.Today.ToUniversalTime();
        private readonly DateTimeOffset _moreThanyearAgoDateTime = DateTime.Today.AddDays(-375).ToUniversalTime();

        public GetYearlySumPeriodTests(ElectricityMarketDatabaseContextFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetYearlySumFull365DaysPeriodTest()
        {
            // arrange
            var parentIdentification = await CreateTestDataAsyncFullYear();

            var dbContext = _fixture.DatabaseManager.CreateDbContext();
            await using var electricityMarketDatabaseContext = dbContext;

            var sut = new MeteringPointRepository(null!, dbContext, null!, null!);
            var res = await sut.GetMeteringPointForSignatureAsync(parentIdentification);

            Assert.NotNull(res);

            // act + assert period of last 365 days.
            var target = new GetYearlySumPeriodHandler(sut);

            var requestPeriod = new Interval(Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-100)), Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-1)));
            var command = new GetYearlySumPeriodCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), requestPeriod);
            var response = await target.Handle(command, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(_yearAgo, response.Value.Start);
            Assert.Equal(_today, response.Value.End);

        }

        [Fact]
        public async Task GetYearlySumLessThanYearStartLaterPeriodTest()
        {
            // arrange
            var parentIdentification = await CreateTestDataAsyncLessThanYearStartLater();

            var dbContext = _fixture.DatabaseManager.CreateDbContext();
            await using var electricityMarketDatabaseContext = dbContext;

            var sut = new MeteringPointRepository(null!, dbContext, null!, null!);
            var res = await sut.GetMeteringPointForSignatureAsync(parentIdentification);

            Assert.NotNull(res);

            // act + assert period of last 150 days.
            var target = new GetYearlySumPeriodHandler(sut);

            var requestPeriod = new Interval(Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-100)), Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-1)));
            var command = new GetYearlySumPeriodCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), requestPeriod);
            var response = await target.Handle(command, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime().AddDays(-150)), response.Value.Start);
            Assert.Equal(_today, response.Value.End);

        }

        [Fact]
        public async Task GetYearlySumMoveOutInLast365DaysTest()
        {
            // arrange
            var parentIdentification = await CreateTestDataAsyncMoveOutInLast365Days();

            var dbContext = _fixture.DatabaseManager.CreateDbContext();
            await using var electricityMarketDatabaseContext = dbContext;

            var sut = new MeteringPointRepository(null!, dbContext, null!, null!);
            var res = await sut.GetMeteringPointForSignatureAsync(parentIdentification);

            Assert.NotNull(res);

            // act + assert period till 150 days ago.
            var target = new GetYearlySumPeriodHandler(sut);

            var requestPeriod = new Interval(Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-100)), Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-1)));
            var command = new GetYearlySumPeriodCommand(res.Identification.Value.ToString(CultureInfo.InvariantCulture), requestPeriod);
            var response = await target.Handle(command, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(_yearAgo, response.Value.Start);
            Assert.Equal(Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime().AddDays(-150)), response.Value.End);
        }

        public Task InitializeAsync() => _fixture.InitializeAsync();

        public Task DisposeAsync() => _fixture.DisposeAsync();

        private async Task<MeteringPointIdentification> CreateTestDataAsyncFullYear()
        {
            await using var dbContext = _fixture.DatabaseManager.CreateDbContext();

            // Parent metering point
            var parentIdentification = Some.MeteringPointIdentification();
            var installationAddress = Some.InstallationAddressEntity();
            await dbContext.InstallationAddresses.AddAsync(installationAddress);
            var meteringPointPeriodEntity = new MeteringPointPeriodEntity()
            {
                ValidFrom = _moreThanyearAgoDateTime,
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
                StartDate = _moreThanyearAgoDateTime,
                EndDate = DateTimeOffset.MaxValue,
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value,
                EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = _moreThanyearAgoDateTime,
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = _moreThanyearAgoDateTime,
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = _balanceSupplier1,
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
            };

            // No endate
            var commercialRelationEntity2 = new CommercialRelationEntity()
            {
                StartDate = _moreThanyearAgoDateTime.AddYears(-3),
                EndDate = _moreThanyearAgoDateTime.AddYears(-2),
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value,
                EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = _moreThanyearAgoDateTime.AddYears(-3),
                                ValidTo = _moreThanyearAgoDateTime.AddYears(-2),
                                CreatedAt = _moreThanyearAgoDateTime.AddYears(-3),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = _balanceSupplier1,
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
            };
            var commercialRelationEntity3 = new CommercialRelationEntity()
            {
                StartDate = _moreThanyearAgoDateTime.AddYears(-5),
                EndDate = _moreThanyearAgoDateTime.AddYears(-4),
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value,
                EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = _moreThanyearAgoDateTime.AddYears(-5),
                                ValidTo = _moreThanyearAgoDateTime.AddYears(-4),
                                CreatedAt = _moreThanyearAgoDateTime.AddYears(-5),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = _balanceSupplier1,
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
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

        private async Task<MeteringPointIdentification> CreateTestDataAsyncLessThanYearStartLater()
        {
            await using var dbContext = _fixture.DatabaseManager.CreateDbContext();

            // Parent metering point
            var parentIdentification = Some.MeteringPointIdentification();
            var installationAddress = Some.InstallationAddressEntity();
            await dbContext.InstallationAddresses.AddAsync(installationAddress);
            var meteringPointPeriodEntity = new MeteringPointPeriodEntity()
            {
                ValidFrom = _moreThanyearAgoDateTime,
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
                StartDate = DateTime.Today.ToUniversalTime().AddDays(-150),
                EndDate = DateTimeOffset.MaxValue,
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value,
                EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = DateTime.Today.ToUniversalTime().AddDays(-150),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = DateTime.Today.ToUniversalTime().AddDays(-150),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = _balanceSupplier1,
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
            };

            await dbContext.MeteringPointPeriods.AddAsync(meteringPointPeriodEntity);
            await dbContext.CommercialRelations.AddAsync(commercialRelationEntity1);
            await dbContext.MeteringPoints.AddAsync(new MeteringPointEntity()
            {
                Identification = parentIdentification.Value,
                MeteringPointPeriods = { meteringPointPeriodEntity },
                CommercialRelations = { commercialRelationEntity1 }
            });

            await dbContext.SaveChangesAsync();
            return parentIdentification;
        }

        private async Task<MeteringPointIdentification> CreateTestDataAsyncMoveOutInLast365Days()
        {
            await using var dbContext = _fixture.DatabaseManager.CreateDbContext();

            // Parent metering point
            var parentIdentification = Some.MeteringPointIdentification();
            var installationAddress = Some.InstallationAddressEntity();
            await dbContext.InstallationAddresses.AddAsync(installationAddress);
            var meteringPointPeriodEntity = new MeteringPointPeriodEntity()
            {
                ValidFrom = _moreThanyearAgoDateTime,
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
                StartDate = _moreThanyearAgoDateTime,
                EndDate = DateTime.Today.ToUniversalTime().AddDays(-150),
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value,
                EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = _moreThanyearAgoDateTime,
                                ValidTo = DateTime.Today.ToUniversalTime().AddDays(-150),
                                CreatedAt = _moreThanyearAgoDateTime,
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = _balanceSupplier1,
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
            };

            // No endate
            var commercialRelationEntity2 = new CommercialRelationEntity()
            {
                StartDate = _moreThanyearAgoDateTime.AddYears(-3),
                EndDate = _moreThanyearAgoDateTime.AddYears(-2),
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value,
                EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = _moreThanyearAgoDateTime.AddYears(-3),
                                ValidTo = _moreThanyearAgoDateTime.AddYears(-2),
                                CreatedAt = _moreThanyearAgoDateTime.AddYears(-3),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = _balanceSupplier1,
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
            };
            var commercialRelationEntity3 = new CommercialRelationEntity()
            {
                StartDate = _moreThanyearAgoDateTime.AddYears(-5),
                EndDate = _moreThanyearAgoDateTime.AddYears(-4),
                EnergySupplier = _balanceSupplier1,
                MeteringPointId = parentIdentification.Value,
                EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = _moreThanyearAgoDateTime.AddYears(-5),
                                ValidTo = _moreThanyearAgoDateTime.AddYears(-4),
                                CreatedAt = _moreThanyearAgoDateTime.AddYears(-5),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = _balanceSupplier1,
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
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
