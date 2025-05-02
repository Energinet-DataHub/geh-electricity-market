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
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Common;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model.MarketParticipant;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using FluentAssertions;
using NodaTime.Extensions;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Repositories;

public class MeteringPointIntegrationRepositoryTests : IClassFixture<ElectricityMarketDatabaseContextFixture>, IAsyncLifetime
{
    private readonly ElectricityMarketDatabaseContextFixture _fixture;

    public MeteringPointIntegrationRepositoryTests(ElectricityMarketDatabaseContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Given_NoMasterData_When_GetMeteringPointMasterDataChangesAsync_Then_NoMasterDataReturned()
    {
        var electricityMarketDatabaseContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointIntegrationRepository(electricityMarketDatabaseContext);

        var result = await sut.GetMeteringPointMasterDataChangesAsync(
                "570715000001552082",
                new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero))
            .ToListAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task
        Given_MinimalMasterData_When_GetMeteringPointMasterDataChangesAsync_Then_NormalisedMasterDataReturned()
    {
        var electricityMarketDatabaseContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointIntegrationRepository(electricityMarketDatabaseContext);

        await SetupMeteringPoint570715000001552082(electricityMarketDatabaseContext);

        var result = await sut.GetMeteringPointMasterDataChangesAsync(
                "570715000001552082",
                new DateTimeOffset(2017, 1, 20, 22, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2017, 1, 20, 23, 0, 0, 0, TimeSpan.Zero))
            .ToListAsync();

        result.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790000701964",
                    GridAccessProvider = "5790000610976",
                    GridAreaCode = new GridAreaCode("543"),
                    Identification = new MeteringPointIdentification("570715000001552082"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT15M"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Production,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2017, 1, 19, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2017, 1, 20, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                });
    }

    [Fact]
    public async Task
        Given_MasterDataWithCustomerRelationsOutOfBounds_When_GetMeteringPointMasterDataChangesAsync_Then_NormalisedMasterDataReturned()
    {
        var electricityMarketDatabaseContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointIntegrationRepository(electricityMarketDatabaseContext);

        await SetupMeteringPoint38277810000000(electricityMarketDatabaseContext);

        var result = await sut.GetMeteringPointMasterDataChangesAsync(
                "38277810000000",
                new DateTimeOffset(2025, 3, 25, 23, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 3, 25, 23, 0, 1, 0, TimeSpan.Zero))
            .ToListAsync();

        result.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Disconnected,
                    EnergySupplier = "5790002424762",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("38277810000000"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Physical,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2025, 3, 25, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = DateTimeOffset.MaxValue.ToInstant(),
                });
    }

    [Fact]
    public async Task
        Given_MasterDataWithMultipleEnergySuppliers_When_GetMeteringPointMasterDataChangesAsync_Then_NormalisedMasterDataReturned()
    {
        var electricityMarketDatabaseContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointIntegrationRepository(electricityMarketDatabaseContext);

        await SetupMeteringPoint571313180400090019(electricityMarketDatabaseContext);

        var result = await sut.GetMeteringPointMasterDataChangesAsync(
                "571313180400090019",
                new DateTimeOffset(2019, 7, 1, 22, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2021, 1, 1, 23, 0, 0, TimeSpan.Zero))
            .ToListAsync();

        result.OrderBy(mpmd => mpmd.ValidFrom)
            .Should()
            .HaveCount(4)
            .And.BeEquivalentTo(
            [
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790002420696",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400090019"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2019, 8, 12, 22, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2020, 2, 17, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790002424762",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400090019"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2020, 2, 17, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2020, 2, 29, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790001687137",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400090019"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2020, 2, 29, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2020, 12, 8, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790002420696",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400090019"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2020, 12, 8, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = DateTimeOffset.MaxValue.ToInstant(),
                },
            ]);
    }

    [Fact]
    public async Task
        Given_ChangesToMasterData_When_GetMeteringPointMasterDataChangesAsync_Then_NormalisedMasterDataReturned()
    {
        var electricityMarketDatabaseContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointIntegrationRepository(electricityMarketDatabaseContext);

        await SetupMeteringPoint578044607691001804(electricityMarketDatabaseContext);

        var result = await sut.GetMeteringPointMasterDataChangesAsync(
                "578044607691001804",
                new DateTimeOffset(2024, 12, 31, 23, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 5, 31, 22, 0, 0, TimeSpan.Zero))
            .ToListAsync();

        result.OrderBy(mpmd => mpmd.ValidFrom)
            .Should()
            .HaveCount(4)
            .And.BeEquivalentTo(
            [
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.New,
                    EnergySupplier = null,
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("578044607691001804"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Physical,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2025, 1, 9, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2025, 1, 12, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.New,
                    EnergySupplier = null,
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("578044607691001804"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Physical,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2025, 1, 12, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2025, 1, 13, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.New,
                    EnergySupplier = "8100000000115",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("578044607691001804"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Physical,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2025, 1, 13, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2025, 2, 4, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "8100000000115",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("578044607691001804"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Physical,
                    Type = MeteringPointType.Consumption,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2025, 2, 4, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = DateTimeOffset.MaxValue.ToInstant(),
                },
            ]);
    }

    [Fact]
    public async Task
        Given_ChangesToMasterDataAndEnergySupplier_When_GetMeteringPointMasterDataChangesAsync_Then_NormalisedMasterDataReturned()
    {
        var electricityMarketDatabaseContext = _fixture.DatabaseManager.CreateDbContext();
        var sut = new MeteringPointIntegrationRepository(electricityMarketDatabaseContext);

        await SetupMeteringPoint571313180400031999(electricityMarketDatabaseContext);

        var result = await sut.GetMeteringPointMasterDataChangesAsync(
                "571313180400031999",
                new DateTimeOffset(2010, 12, 31, 23, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero))
            .ToListAsync();

        result.OrderBy(mpmd => mpmd.ValidFrom)
            .Should()
            .HaveCount(5)
            .And.BeEquivalentTo(
            [
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790002473517",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400031999"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT1H"),
                    SubType = MeteringPointSubType.Physical,
                    Type = MeteringPointType.Production,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2018, 11, 15, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2021, 9, 19, 22, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790002473517",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400031999"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT15M"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Production,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2021, 9, 19, 22, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2021, 10, 11, 22, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790001687137",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400031999"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT15M"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Production,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2021, 10, 11, 22, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2021, 12, 8, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790001687137",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400031999"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT15M"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Production,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2021, 12, 8, 23, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = new DateTimeOffset(2023, 8, 28, 22, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                },
                new MeteringPointMasterData
                {
                    ConnectionState = ConnectionState.Connected,
                    EnergySupplier = "5790001103095",
                    GridAccessProvider = "8500000000502",
                    GridAreaCode = new GridAreaCode("804"),
                    Identification = new MeteringPointIdentification("571313180400031999"),
                    NeighborGridAreaOwners = [],
                    ParentIdentification = null,
                    ProductId = ProductId.EnergyActive,
                    Resolution = new Resolution("PT15M"),
                    SubType = MeteringPointSubType.Virtual,
                    Type = MeteringPointType.Production,
                    Unit = MeasureUnit.kWh,
                    ValidFrom = new DateTimeOffset(2023, 8, 28, 22, 0, 0, 0, TimeSpan.Zero).ToInstant(),
                    ValidTo = DateTimeOffset.MaxValue.ToInstant(),
                },
            ]);
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private static async Task SetupMeteringPoint571313180400031999(ElectricityMarketDatabaseContext context)
    {
        await context.Actors.AddAsync(
            new ActorEntity
            {
                OrganizationId = Guid.NewGuid(),
                ActorId = Guid.Parse("a717bc61-dacb-4f6d-9acd-cdaaa54d1677"),
                ActorNumber = "8500000000502",
                Status = ActorStatus.Active,
                Name = "Test Actor",
                IsFas = false,
                MarketRole = new MarketRoleEntity
                {
                    ActorId = Guid.Parse("a717bc61-dacb-4f6d-9acd-cdaaa54d1677"),
                    Function = EicFunction.GridAccessProvider,
                    GridAreas =
                    {
                        new MarketRoleGridAreaEntity
                        {
                            GridAreaId = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                            MarketRoleId = Guid.Parse("c22160ec-29a0-4c7f-c1d9-08dcdba22339"),
                            MeteringPointTypes =
                            {
                                new MeteringPointTypeEntity
                                {
                                    MarketRoleGridAreaId =
                                        Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                                    MeteringTypeId = 14,
                                },
                            },
                        },
                    },
                },
            });

        await context.GridAreas.AddAsync(
            new GridAreaEntity
            {
                Id = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                Code = "804",
                Name = "Test Grid Area",
                PriceAreaCode = PriceAreaCode.Dk1,
                ValidFrom = new DateTimeOffset(2014, 9, 30, 22, 0, 0, 0, TimeSpan.Zero),
                FullFlexDate = new DateTimeOffset(2019, 12, 31, 23, 0, 0, 0, TimeSpan.Zero),
            });

        await context.InstallationAddresses.AddRangeAsync(
            new InstallationAddressEntity
            {
                // Id = 37878540000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37878540001,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37878540002,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37878540003,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37878540004,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37878540005,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            });

        await context.MeteringPoints.AddAsync(
            new MeteringPointEntity
            {
                Identification = "571313180400031999",

                // Id = 3787854,
                Version = new DateTimeOffset(2025, 4, 9, 12, 54, 29, 0, TimeSpan.Zero),
                CommercialRelations =
                {
                    new CommercialRelationEntity
                    {
                        // Id = 3787854000,
                        MeteringPointId = 3787854,
                        EnergySupplier = "5790002473517",
                        StartDate = new DateTimeOffset(2018, 11, 15, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2021, 10, 11, 22, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2021, 9, 20, 8, 45, 57, 723, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3787854000000,
                                CommercialRelationId = 3787854000,
                                ValidFrom = new DateTimeOffset(2018, 11, 15, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2021, 9, 20, 8, 45, 57, 723, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790002473517",
                                BusinessTransactionDosId = 249339227,
                                TransactionType = "CREATEMP",
                            },
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 3787854001,
                        MeteringPointId = 3787854,
                        EnergySupplier = "5790001687137",
                        StartDate = new DateTimeOffset(2021, 10, 11, 22, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2021, 12, 8, 23, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2021, 12, 9, 7, 20, 37, 726, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3787854001000,
                                CommercialRelationId = 3787854001,
                                ValidFrom = new DateTimeOffset(2021, 10, 11, 22, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2021, 12, 9, 7, 20, 37, 727, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790001687137",
                                BusinessTransactionDosId = 298896419,
                                TransactionType = "MOVEINES",
                            },
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 3787854002,
                        MeteringPointId = 3787854,
                        EnergySupplier = "5790001687137",
                        StartDate = new DateTimeOffset(2021, 12, 8, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2023, 8, 28, 22, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2021, 12, 20, 11, 23, 24, 410, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3787854002000,
                                CommercialRelationId = 3787854002,
                                ValidFrom = new DateTimeOffset(2021, 12, 8, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2021, 12, 20, 11, 23, 24, 410, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790001687137",
                                BusinessTransactionDosId = 301647167,
                                TransactionType = "MOVEINES",
                            },
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 3787854003,
                        MeteringPointId = 3787854,
                        EnergySupplier = "5790001103095",
                        StartDate = new DateTimeOffset(2023, 8, 28, 22, 0, 0, 0, TimeSpan.Zero),
                        EndDate = DateTimeOffset.MaxValue,
                        ModifiedAt = new DateTimeOffset(2023, 10, 24, 13, 31, 38, 795, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3787854003000,
                                CommercialRelationId = 3787854003,
                                ValidFrom = new DateTimeOffset(2023, 8, 28, 22, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2023, 10, 24, 13, 31, 38, 795, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790001103095",
                                BusinessTransactionDosId = 326247367,
                                TransactionType = "CHANGESUP",
                            },
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3787854003002,
                                CommercialRelationId = 3787854003,
                                ValidFrom = new DateTimeOffset(2023, 9, 14, 22, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2023, 10, 24, 13, 31, 38, 795, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790001103095",
                                BusinessTransactionDosId = 326254453,
                                TransactionType = "MSDCNSSBM",
                            },
                        ],
                    },
                },
                MeteringPointPeriods =
                {
                    new MeteringPointPeriodEntity
                    {
                        // Id = 37878540000,
                        MeteringPointId = 3787854,
                        ValidFrom = new DateTimeOffset(2018, 11, 15, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        RetiredById = 2,
                        RetiredAt = new DateTimeOffset(2025, 4, 9, 12, 54, 29, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2021, 9, 20, 8, 45, 57, 723, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Physical",
                        ConnectionState = "Connected",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        DisconnectionType = "RemoteDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = false,
                        MeasureUnit = "kWh",
                        PowerLimitKw = 22,
                        PowerLimitA = 32,
                        MeterNumber = "00150006",
                        SettlementGroup = 0,
                        PowerPlantGsrn = "571313126900150006",
                        InstallationAddressId = 1,
                        MeteringPointStateId = 221802051,
                        BusinessTransactionDosId = 249339227,
                        TransactionType = "CREATEMP",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 37878540001,
                        MeteringPointId = 3787854,
                        ValidFrom = new DateTimeOffset(2018, 11, 15, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = new DateTimeOffset(2021, 9, 19, 22, 0, 0, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2021, 9, 20, 8, 45, 57, 723, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Physical",
                        ConnectionState = "Connected",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        DisconnectionType = "RemoteDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = false,
                        MeasureUnit = "kWh",
                        PowerLimitKw = 22,
                        PowerLimitA = 32,
                        MeterNumber = "00150006",
                        SettlementGroup = 0,
                        PowerPlantGsrn = "571313126900150006",
                        InstallationAddressId = 2,
                        MeteringPointStateId = 221802051,
                        BusinessTransactionDosId = 249339227,
                        TransactionType = "CREATEMP",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 37878540002,
                        MeteringPointId = 3787854,
                        ValidFrom = new DateTimeOffset(2021, 9, 19, 22, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        RetiredById = 4,
                        RetiredAt = new DateTimeOffset(2025, 4, 9, 12, 54, 29, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2021, 10, 13, 7, 10, 37, 836, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Virtual",
                        ConnectionState = "Connected",
                        Resolution = "PT15M",
                        GridAreaCode = "804",
                        ConnectionType = "Installation",
                        DisconnectionType = "ManualDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = false,
                        MeasureUnit = "kWh",
                        AssetType = "HydroelectricPower",
                        Capacity = "11",
                        PowerLimitKw = 100,
                        PowerLimitA = 32,
                        SettlementGroup = 3,
                        PowerPlantGsrn = "571313126900150006",
                        InstallationAddressId = 3,
                        MeteringPointStateId = 221806152,
                        BusinessTransactionDosId = 298560636,
                        TransactionType = "STPMETER",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 37878540003,
                        MeteringPointId = 3787854,
                        ValidFrom = new DateTimeOffset(2021, 9, 19, 22, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        RetiredById = 5,
                        RetiredAt = new DateTimeOffset(2025, 4, 9, 12, 54, 29, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2021, 10, 13, 7, 10, 37, 836, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Virtual",
                        ConnectionState = "Connected",
                        Resolution = "PT15M",
                        GridAreaCode = "804",
                        ConnectionType = "Installation",
                        DisconnectionType = "ManualDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = false,
                        MeasureUnit = "kWh",
                        AssetType = "HydroelectricPower",
                        Capacity = "11",
                        PowerLimitKw = 100,
                        PowerLimitA = 32,
                        SettlementGroup = 3,
                        PowerPlantGsrn = "571313126900150006",
                        InstallationAddressId = 4,
                        MeteringPointStateId = 221806152,
                        BusinessTransactionDosId = 298560638,
                        TransactionType = "MSTDATSBM",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 37878540004,
                        MeteringPointId = 3787854,
                        ValidFrom = new DateTimeOffset(2021, 9, 19, 22, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = new DateTimeOffset(2023, 8, 28, 22, 0, 0, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2021, 10, 13, 7, 10, 37, 836, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Virtual",
                        ConnectionState = "Connected",
                        Resolution = "PT15M",
                        GridAreaCode = "804",
                        ConnectionType = "Installation",
                        DisconnectionType = "ManualDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = false,
                        MeasureUnit = "kWh",
                        AssetType = "HydroelectricPower",
                        Capacity = "11",
                        PowerLimitKw = 100,
                        PowerLimitA = 32,
                        SettlementGroup = 3,
                        PowerPlantGsrn = "571313126900150006",
                        InstallationAddressId = 5,
                        MeteringPointStateId = 221806152,
                        BusinessTransactionDosId = 298560638,
                        TransactionType = "MSTDATSBM",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 37878540005,
                        MeteringPointId = 3787854,
                        ValidFrom = new DateTimeOffset(2023, 8, 28, 22, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        CreatedAt = new DateTimeOffset(2023, 10, 24, 13, 31, 38, 795, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Virtual",
                        ConnectionState = "Connected",
                        Resolution = "PT15M",
                        GridAreaCode = "804",
                        ConnectionType = "Installation",
                        DisconnectionType = "ManualDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = false,
                        MeasureUnit = "kWh",
                        AssetType = "HydroelectricPower",
                        Capacity = "11",
                        PowerLimitKw = 100,
                        PowerLimitA = 32,
                        SettlementGroup = 2,
                        PowerPlantGsrn = "571313126900150006",
                        InstallationAddressId = 6,
                        MeteringPointStateId = 223560880,
                        BusinessTransactionDosId = 327735676,
                        TransactionType = "MSTDATSBM",
                    },
                },
            });

        await context.SaveChangesAsync();
    }

    private static async Task SetupMeteringPoint571313180400090019(ElectricityMarketDatabaseContext context)
    {
        await context.Actors.AddAsync(
            new ActorEntity
            {
                OrganizationId = Guid.NewGuid(),
                ActorId = Guid.Parse("a717bc61-dacb-4f6d-9acd-cdaaa54d1677"),
                ActorNumber = "8500000000502",
                Status = ActorStatus.Active,
                Name = "Test Actor",
                IsFas = false,
                MarketRole = new MarketRoleEntity
                {
                    ActorId = Guid.Parse("a717bc61-dacb-4f6d-9acd-cdaaa54d1677"),
                    Function = EicFunction.GridAccessProvider,
                    GridAreas =
                    {
                        new MarketRoleGridAreaEntity
                        {
                            GridAreaId = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                            MarketRoleId = Guid.Parse("c22160ec-29a0-4c7f-c1d9-08dcdba22339"),
                            MeteringPointTypes =
                            {
                                new MeteringPointTypeEntity
                                {
                                    MarketRoleGridAreaId =
                                        Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                                    MeteringTypeId = 14,
                                },
                            },
                        },
                    },
                },
            });

        await context.GridAreas.AddAsync(
            new GridAreaEntity
            {
                Id = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                Code = "804",
                Name = "Test Grid Area",
                PriceAreaCode = PriceAreaCode.Dk1,
                ValidFrom = new DateTimeOffset(2014, 9, 30, 22, 0, 0, 0, TimeSpan.Zero),
                FullFlexDate = new DateTimeOffset(2019, 12, 31, 23, 0, 0, 0, TimeSpan.Zero),
            });

        await context.InstallationAddresses.AddAsync(
            new InstallationAddressEntity
            {
                // Id = 37906250000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            });

        await context.MeteringPoints.AddAsync(
            new MeteringPointEntity
            {
                Identification = "571313180400090019",

                // Id = 3790625,
                Version = new DateTimeOffset(2025, 4, 7, 8, 38, 13, 0, TimeSpan.Zero),
                CommercialRelations =
                {
                    new CommercialRelationEntity
                    {
                        // Id = 3790625000,
                        MeteringPointId = 3790625,
                        EnergySupplier = "5790002420696",
                        StartDate = new DateTimeOffset(2019, 8, 12, 22, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2020, 2, 17, 23, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2020, 2, 18, 16, 20, 38, 552, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3790625000000,
                                CommercialRelationId = 3790625000,
                                ValidFrom = new DateTimeOffset(2019, 8, 12, 22, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2020, 2, 18, 16, 20, 38, 552, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790002420696",
                                BusinessTransactionDosId = 262022906,
                                TransactionType = "CREATEMP",
                            },
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 3790625001,
                        MeteringPointId = 3790625,
                        EnergySupplier = "5790002424762",
                        StartDate = new DateTimeOffset(2020, 2, 17, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2020, 2, 29, 23, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2020, 3, 2, 14, 10, 38, 552, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3790625001000,
                                CommercialRelationId = 3790625000,
                                ValidFrom = new DateTimeOffset(2020, 2, 17, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2020, 3, 2, 14, 10, 38, 552, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790002424762",
                                BusinessTransactionDosId = 270889573,
                                TransactionType = "CREATEMP",
                            },
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 3790625002,
                        MeteringPointId = 3790625,
                        EnergySupplier = "5790001687137",
                        StartDate = new DateTimeOffset(2020, 2, 29, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2020, 12, 8, 23, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2020, 12, 16, 10, 45, 42, 552, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3790625002000,
                                CommercialRelationId = 3790625000,
                                ValidFrom = new DateTimeOffset(2020, 2, 29, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2020, 12, 16, 10, 45, 42, 552, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790001687137",
                                BusinessTransactionDosId = 271746030,
                                TransactionType = "CREATEMP",
                            },
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 3790625003,
                        MeteringPointId = 3790625,
                        EnergySupplier = "5790002420696",
                        StartDate = new DateTimeOffset(2020, 12, 8, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = DateTimeOffset.MaxValue,
                        ModifiedAt = new DateTimeOffset(2020, 12, 16, 10, 45, 42, 552, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3790625003000,
                                CommercialRelationId = 3790625003,
                                ValidFrom = new DateTimeOffset(2020, 12, 8, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2020, 12, 16, 10, 45, 42, 552, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790002420696",
                                BusinessTransactionDosId = 285076808,
                                TransactionType = "CREATEMP",
                            },
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3790625003001,
                                CommercialRelationId = 3790625003,
                                ValidFrom = new DateTimeOffset(2020, 12, 8, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2020, 12, 16, 10, 45, 42, 552, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790002420696",
                                BusinessTransactionDosId = 285076808,
                                TransactionType = "CREATEMP",
                            },
                        ],
                    },
                },
                MeteringPointPeriods =
                {
                    new MeteringPointPeriodEntity
                    {
                        // Id = 37906250000,
                        MeteringPointId = 3790625,
                        ValidFrom = new DateTimeOffset(2019, 8, 12, 22, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        CreatedAt = new DateTimeOffset(2020, 2, 18, 16, 20, 38, 552, TimeSpan.Zero),
                        Type = "Consumption",
                        SubType = "Virtual",
                        ConnectionState = "Connected",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        ConnectionType = "Installation",
                        DisconnectionType = "ManualDisconnection",
                        Product = "EnergyActive",
                        MeasureUnit = "kWh",
                        Capacity = "200",
                        SettlementGroup = 6,
                        ScheduledMeterReadingMonth = 1,
                        PowerPlantGsrn = "571313180400090019",
                        SettlementMethod = "FlexSettled",
                        InstallationAddressId = 1,
                        MeteringPointStateId = 218306143,
                        BusinessTransactionDosId = 262022906,
                        TransactionType = "MOVEINES",
                    },
                },
            });

        await context.SaveChangesAsync();
    }

    private static async Task SetupMeteringPoint570715000001552082(ElectricityMarketDatabaseContext context)
    {
        await context.Actors.AddAsync(
            new ActorEntity
            {
                OrganizationId = Guid.NewGuid(),
                ActorId = Guid.Parse("c0fbeb79-45d5-497a-becf-780b4d849944"),
                ActorNumber = "5790000610976",
                Status = ActorStatus.Active,
                Name = "Test Actor",
                IsFas = false,
                MarketRole = new MarketRoleEntity
                {
                    ActorId = Guid.Parse("c0fbeb79-45d5-497a-becf-780b4d849944"),
                    Function = EicFunction.GridAccessProvider,
                    GridAreas =
                    {
                        new MarketRoleGridAreaEntity
                        {
                            GridAreaId = Guid.Parse("1dcce894-0990-475a-85bf-5520a9b872ce"),
                            MarketRoleId = Guid.Parse("51cc77b3-1cf4-44cf-dddc-08dd1043f288"),
                            MeteringPointTypes =
                            {
                                new MeteringPointTypeEntity
                                {
                                    MarketRoleGridAreaId =
                                        Guid.Parse("1dcce894-0990-475a-85bf-5520a9b872ce"),
                                    MeteringTypeId = 14,
                                },
                            },
                        },
                    },
                },
            });

        await context.GridAreas.AddAsync(
            new GridAreaEntity
            {
                Id = Guid.Parse("1dcce894-0990-475a-85bf-5520a9b872ce"),
                Code = "543",
                Name = "Test Grid Area",
                PriceAreaCode = PriceAreaCode.Dk1,
                ValidFrom = new DateTimeOffset(2018, 2, 21, 23, 0, 0, 0, TimeSpan.Zero),
                FullFlexDate = new DateTimeOffset(2019, 12, 31, 23, 0, 0, 0, TimeSpan.Zero),
            });

        await context.InstallationAddresses.AddRangeAsync(
            new InstallationAddressEntity
            {
                // Id = 516430002,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 41295070001,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            });

        await context.MeteringPoints.AddAsync(
            new MeteringPointEntity
            {
                Identification = "570715000001552082",

                // Id = 51643,
                Version = new DateTimeOffset(2025, 4, 7, 8, 18, 3, 0, TimeSpan.Zero),
                CommercialRelations =
                {
                    new CommercialRelationEntity
                    {
                        // Id = 51643000,
                        MeteringPointId = 51643,
                        EnergySupplier = "5790000701964",
                        StartDate = new DateTimeOffset(2017, 1, 19, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = DateTimeOffset.MaxValue,
                        ModifiedAt = new DateTimeOffset(2017, 1, 23, 10, 46, 13, 552, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 51643000000,
                                CommercialRelationId = 51643000,
                                ValidFrom = new DateTimeOffset(2017, 1, 19, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2017, 1, 23, 10, 46, 13, 552, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790000701964",
                                BusinessTransactionDosId = 194195746,
                                TransactionType = "CREATEMP",
                            },
                        ],
                    },
                },
                MeteringPointPeriods =
                {
                    new MeteringPointPeriodEntity
                    {
                        // Id = 516430000,
                        MeteringPointId = 51643,
                        ValidFrom = new DateTimeOffset(2017, 1, 19, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = new DateTimeOffset(2017, 1, 20, 23, 0, 0, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2017, 1, 23, 10, 46, 13, 552, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Virtual",
                        ConnectionState = "Connected",
                        Resolution = "PT15M",
                        GridAreaCode = "543",
                        ConnectionType = "Installation",
                        DisconnectionType = "ManualDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = true,
                        MeasureUnit = "kWh",
                        Capacity = "6",
                        SettlementGroup = 4,
                        PowerPlantGsrn = "570715000001552082",
                        InstallationAddressId = 1,
                        MeteringPointStateId = 188242634,
                        BusinessTransactionDosId = 194336484,
                        TransactionType = "CONNECTMP",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 516430000,
                        MeteringPointId = 51643,
                        ValidFrom = new DateTimeOffset(2017, 1, 20, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        CreatedAt = new DateTimeOffset(2017, 1, 24, 10, 46, 13, 552, TimeSpan.Zero),
                        Type = "Production",
                        SubType = "Virtual",
                        ConnectionState = "Connected",
                        Resolution = "PT15M",
                        GridAreaCode = "543",
                        ConnectionType = "Installation",
                        DisconnectionType = "ManualDisconnection",
                        Product = "EnergyActive",
                        ProductObligation = true,
                        MeasureUnit = "kWh",
                        Capacity = "6",
                        SettlementGroup = 4,
                        PowerPlantGsrn = "570715000001552082",
                        InstallationAddressId = 2,
                        MeteringPointStateId = 188242634,
                        BusinessTransactionDosId = 194336484,
                        TransactionType = "CONNECTMP",
                    },
                },
            });

        await context.SaveChangesAsync();
    }

    private static async Task SetupMeteringPoint578044607691001804(ElectricityMarketDatabaseContext context)
    {
        await context.Actors.AddAsync(
            new ActorEntity
            {
                OrganizationId = Guid.NewGuid(),
                ActorId = Guid.Parse("3ba615a1-6bfe-4952-a994-08dc3d253234"),
                ActorNumber = "8500000000502",
                Status = ActorStatus.Active,
                Name = "Test Actor",
                IsFas = false,
                MarketRole = new MarketRoleEntity
                {
                    ActorId = Guid.Parse("3ba615a1-6bfe-4952-a994-08dc3d253234"),
                    Function = EicFunction.GridAccessProvider,
                    GridAreas =
                    {
                        new MarketRoleGridAreaEntity
                        {
                            GridAreaId = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                            MarketRoleId = Guid.Parse("c22160ec-29a0-4c7f-c1d9-08dcdba22339"),
                            MeteringPointTypes =
                            {
                                new MeteringPointTypeEntity
                                {
                                    MarketRoleGridAreaId =
                                        Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                                    MeteringTypeId = 14,
                                },
                            },
                        },
                    },
                },
            });

        await context.GridAreas.AddAsync(
            new GridAreaEntity
            {
                Id = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                Code = "804",
                Name = "Test Grid Area",
                PriceAreaCode = PriceAreaCode.Dk1,
                ValidFrom = new DateTimeOffset(2014, 9, 30, 22, 0, 0, 0, TimeSpan.Zero),
                FullFlexDate = new DateTimeOffset(2019, 12, 31, 23, 0, 0, 0, TimeSpan.Zero),
            });

        await context.InstallationAddresses.AddRangeAsync(
            new InstallationAddressEntity
            {
                // Id = 41295070000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 41295070001,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 41295070002,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
                CitySubdivisionName = "Test City Subdivision",
            },
            new InstallationAddressEntity
            {
                // Id = 41295070003,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
                CitySubdivisionName = "Test City Subdivision",
            },
            new InstallationAddressEntity
            {
                // Id = 41295070004,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
                CitySubdivisionName = "Test City Subdivision",
            });

        await context.MeteringPoints.AddAsync(
            new MeteringPointEntity
            {
                Identification = "578044607691001804",

                // Id = 4129507,
                Version = new DateTimeOffset(2025, 4, 8, 11, 53, 25, 0, TimeSpan.Zero),
                CommercialRelations =
                {
                    new CommercialRelationEntity
                    {
                        // Id = 4129507000,
                        MeteringPointId = 4129507,
                        EnergySupplier = "8100000000108",
                        StartDate = new DateTimeOffset(2025, 1, 16, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2025, 1, 16, 23, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2025, 2, 6, 11, 17, 35, 137, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 4129507000000,
                                CommercialRelationId = 4129507000,
                                ValidFrom = new DateTimeOffset(2025, 1, 16, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2025, 2, 6, 11, 17, 35, 137, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "8100000000108",
                                BusinessTransactionDosId = 339745803,
                                TransactionType = "MOVEINES",
                            },
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 4129507000001,
                                CommercialRelationId = 4129507000,
                                ValidFrom = new DateTimeOffset(2025, 1, 16, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2025, 2, 6, 11, 17, 35, 137, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "8100000000108",
                                BusinessTransactionDosId = 339745803,
                                TransactionType = "MOVEINES",
                            },
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 4129507001,
                        MeteringPointId = 4129507,
                        EnergySupplier = "8100000000115",
                        StartDate = new DateTimeOffset(2025, 1, 13, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = DateTimeOffset.MaxValue,
                        ModifiedAt = new DateTimeOffset(2025, 1, 16, 23, 10, 39, 82, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 4129507001000,
                                CommercialRelationId = 4129507001,
                                ValidFrom = new DateTimeOffset(2025, 1, 13, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2025, 1, 16, 23, 10, 39, 82, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "8100000000115",
                                BusinessTransactionDosId = 339746897,
                                TransactionType = "MOVEINES",
                            },
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 4129507001001,
                                CommercialRelationId = 4129507001,
                                ValidFrom = new DateTimeOffset(2025, 1, 13, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2025, 1, 16, 23, 10, 39, 82, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "8100000000115",
                                BusinessTransactionDosId = 339746897,
                                TransactionType = "MOVEINES",
                            },
                        ],
                    },
                },
                MeteringPointPeriods =
                {
                    new MeteringPointPeriodEntity
                    {
                        // Id = 41295070000,
                        MeteringPointId = 4129507,
                        ValidFrom = new DateTimeOffset(2025, 1, 9, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        RetiredAt = new DateTimeOffset(2025, 4, 8, 11, 53, 25, 0, TimeSpan.Zero),
                        RetiredById = 2,
                        CreatedAt = new DateTimeOffset(2015, 1, 13, 8, 26, 46, 235, TimeSpan.Zero),
                        Type = "Consumption",
                        SubType = "Physical",
                        ConnectionState = "New",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        DisconnectionType = "RemoteDisconnection",
                        Product = "EnergyActive",
                        MeasureUnit = "kWh",
                        MeterNumber = "123456",
                        SettlementGroup = 0,
                        SettlementMethod = "FlexSettled",
                        InstallationAddressId = 1,
                        MeteringPointStateId = 224108939,
                        BusinessTransactionDosId = 339745638,
                        TransactionType = "CREATEMP",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 41295070001,
                        MeteringPointId = 4129507,
                        ValidFrom = new DateTimeOffset(2025, 1, 9, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = new DateTimeOffset(2025, 1, 12, 23, 0, 0, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2015, 1, 13, 8, 26, 46, 235, TimeSpan.Zero),
                        Type = "Consumption",
                        SubType = "Physical",
                        ConnectionState = "New",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        DisconnectionType = "RemoteDisconnection",
                        Product = "EnergyActive",
                        MeasureUnit = "kWh",
                        MeterNumber = "123456",
                        SettlementGroup = 0,
                        SettlementMethod = "FlexSettled",
                        InstallationAddressId = 2,
                        MeteringPointStateId = 224108939,
                        BusinessTransactionDosId = 339745638,
                        TransactionType = "CREATEMP",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 41295070002,
                        MeteringPointId = 4129507,
                        ValidFrom = new DateTimeOffset(2025, 1, 12, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        RetiredAt = new DateTimeOffset(2025, 4, 8, 11, 53, 25, 0, TimeSpan.Zero),
                        RetiredById = 4,
                        CreatedAt = new DateTimeOffset(2015, 1, 14, 12, 45, 38, 30, TimeSpan.Zero),
                        Type = "Consumption",
                        SubType = "Physical",
                        ConnectionState = "New",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        DisconnectionType = "RemoteDisconnection",
                        Product = "EnergyActive",
                        MeasureUnit = "kWh",
                        MeterNumber = "123456",
                        SettlementGroup = 0,
                        SettlementMethod = "FlexSettled",
                        InstallationAddressId = 3,
                        MeteringPointStateId = 224109748,
                        BusinessTransactionDosId = 339745655,
                        TransactionType = "MSTDATSBM",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 41295070003,
                        MeteringPointId = 4129507,
                        ValidFrom = new DateTimeOffset(2025, 1, 12, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = new DateTimeOffset(2025, 2, 4, 23, 0, 0, 0, TimeSpan.Zero),
                        CreatedAt = new DateTimeOffset(2015, 1, 14, 12, 45, 38, 30, TimeSpan.Zero),
                        Type = "Consumption",
                        SubType = "Physical",
                        ConnectionState = "New",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        DisconnectionType = "RemoteDisconnection",
                        Product = "EnergyActive",
                        MeasureUnit = "kWh",
                        MeterNumber = "123456",
                        SettlementGroup = 0,
                        SettlementMethod = "FlexSettled",
                        InstallationAddressId = 4,
                        MeteringPointStateId = 224109748,
                        BusinessTransactionDosId = 339745655,
                        TransactionType = "MSTDATSBM",
                    },
                    new MeteringPointPeriodEntity
                    {
                        // Id = 41295070004,
                        MeteringPointId = 4129507,
                        ValidFrom = new DateTimeOffset(2025, 2, 4, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
                        CreatedAt = new DateTimeOffset(2015, 2, 6, 11, 17, 35, 137, TimeSpan.Zero),
                        Type = "Consumption",
                        SubType = "Physical",
                        ConnectionState = "Connected",
                        Resolution = "PT1H",
                        GridAreaCode = "804",
                        DisconnectionType = "RemoteDisconnection",
                        Product = "EnergyActive",
                        MeasureUnit = "kWh",
                        MeterNumber = "123456",
                        SettlementGroup = 0,
                        SettlementMethod = "FlexSettled",
                        InstallationAddressId = 5,
                        MeteringPointStateId = 224122602,
                        BusinessTransactionDosId = 339798525,
                        TransactionType = "CONNECTMP",
                    },
                },
            });

        await context.SaveChangesAsync();
    }

    private static async Task SetupMeteringPoint38277810000000(ElectricityMarketDatabaseContext context)
    {
        await context.Actors.AddAsync(
            new ActorEntity
            {
                OrganizationId = Guid.NewGuid(),
                ActorId = Guid.Parse("a717bc61-dacb-4f6d-9acd-cdaaa54d1677"),
                ActorNumber = "8500000000502",
                Status = ActorStatus.Active,
                Name = "Test Actor",
                IsFas = false,
                MarketRole = new MarketRoleEntity
                {
                    ActorId = Guid.Parse("a717bc61-dacb-4f6d-9acd-cdaaa54d1677"),
                    Function = EicFunction.GridAccessProvider,
                    GridAreas =
                    {
                        new MarketRoleGridAreaEntity
                        {
                            GridAreaId = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                            MarketRoleId = Guid.Parse("c22160ec-29a0-4c7f-c1d9-08dcdba22339"),
                            MeteringPointTypes =
                            {
                                new MeteringPointTypeEntity
                                {
                                    MarketRoleGridAreaId =
                                        Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                                    MeteringTypeId = 14,
                                },
                            },
                        },
                    },
                },
            });

        await context.GridAreas.AddAsync(
            new GridAreaEntity
            {
                Id = Guid.Parse("68029af4-617f-4815-bbe5-ecc7510af51f"),
                Code = "804",
                Name = "Test Grid Area",
                PriceAreaCode = PriceAreaCode.Dk1,
                ValidFrom = new DateTimeOffset(2014, 9, 30, 22, 0, 0, 0, TimeSpan.Zero),
                FullFlexDate = new DateTimeOffset(2019, 12, 31, 23, 0, 0, 0, TimeSpan.Zero),
            });

        await context.InstallationAddresses.AddRangeAsync(
            new InstallationAddressEntity
            {
                // Id = 37906250000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37906250000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37906250000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37906250000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            },
            new InstallationAddressEntity
            {
                // Id = 37906250000,
                StreetCode = "5695",
                StreetName = "Test Road",
                BuildingNumber = "42",
                CityName = "Test City",
                WashInstructions = "Washable",
                CountryCode = "DK",
                PostCode = "9999",
                MunicipalityCode = "1024",
            });

        await context.MeteringPoints.AddAsync(
            new MeteringPointEntity
            {
                Identification = "38277810000000",

                // Id = 3790625,
                Version = new DateTimeOffset(2025, 4, 7, 8, 38, 13, 0, TimeSpan.Zero),
                CommercialRelations =
                {
                    new CommercialRelationEntity
                    {
                        // Id = 3790625000,
                        MeteringPointId = 3790625,
                        EnergySupplier = "5790002420696",
                        StartDate = new DateTimeOffset(2025, 3, 18, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2025, 3, 24, 23, 0, 0, 0, TimeSpan.Zero),
                        ModifiedAt = new DateTimeOffset(2020, 2, 18, 16, 20, 38, 552, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                                new EnergySupplyPeriodEntity
                              {
                                  // Id = 3790625000000,
                                  CommercialRelationId = 3790625000,
                                  ValidFrom = new DateTimeOffset(2019, 8, 12, 22, 0, 0, 0, TimeSpan.Zero),
                                  ValidTo = DateTimeOffset.MaxValue,
                                  CreatedAt = new DateTimeOffset(2020, 2, 18, 16, 20, 38, 552, TimeSpan.Zero),
                                  WebAccessCode = "webAccessCode",
                                  EnergySupplier = "5790002420696",
                                  BusinessTransactionDosId = 262022906,
                                  TransactionType = "CREATEMP",
                              },
                              new EnergySupplyPeriodEntity
                              {
                                  // Id = 38277810000000,
                                  CommercialRelationId = 3827781000,
                                  ValidFrom = new DateTimeOffset(2025, 3, 18, 23, 0, 0, 0, TimeSpan.Zero),
                                  ValidTo = DateTimeOffset.MaxValue,
                                  RetiredAt = new DateTimeOffset(2025, 5, 1, 9, 40, 10, 700, TimeSpan.Zero),
                                  CreatedAt = new DateTimeOffset(2025, 3, 26, 14, 9, 30, 828, TimeSpan.Zero),
                                  WebAccessCode = "2cyxmj4c",
                                  EnergySupplier = "8100000000108",
                                  BusinessTransactionDosId = 341696027,
                                  TransactionType = "MOVEINES"
                              },
                              new EnergySupplyPeriodEntity
                              {
                                  // Id = 38277810000001,
                                  CommercialRelationId = 3827781000,
                                  ValidFrom = new DateTimeOffset(2025, 3, 24, 23, 0, 0, 0, TimeSpan.Zero),
                                  ValidTo = DateTimeOffset.MaxValue,
                                  RetiredAt = new DateTimeOffset(2025, 5, 1, 9, 40, 10, 700, TimeSpan.Zero),
                                  CreatedAt = new DateTimeOffset(2025, 3, 25, 15, 15, 38, 248, TimeSpan.Zero),
                                  WebAccessCode = "webAccessCode",
                                  EnergySupplier = "8100000000108",
                                  BusinessTransactionDosId = 341696030,
                                  TransactionType = "ENDSUPPLY"
                              },
                              new EnergySupplyPeriodEntity
                              {
                                  // Id = 38277810000002,
                                  CommercialRelationId = 3827781000,
                                  ValidFrom = new DateTimeOffset(2025, 3, 18, 23, 0, 0, 0, TimeSpan.Zero),
                                  ValidTo = new DateTimeOffset(2025, 3, 24, 23, 0, 0, 0, TimeSpan.Zero),
                                  RetiredAt = new DateTimeOffset(2025, 3, 26, 14, 9, 30, 828, TimeSpan.Zero),
                                  CreatedAt = new DateTimeOffset(2025, 3, 26, 14, 9, 30, 828, TimeSpan.Zero),
                                  WebAccessCode = "2cyxmj4c",
                                  EnergySupplier = "8100000000108",
                                  BusinessTransactionDosId = 341696027,
                                  TransactionType = "MOVEINES"
                              },
                              new EnergySupplyPeriodEntity
                              {
                                  // Id = 38277810000003,
                                  CommercialRelationId = 3827781000,
                                  ValidFrom = new DateTimeOffset(2025, 3, 24, 23, 0, 0, 0, TimeSpan.Zero),
                                  ValidTo = new DateTimeOffset(2025, 3, 25, 23, 0, 0, 0, TimeSpan.Zero),
                                  RetiredAt = new DateTimeOffset(2025, 3, 25, 15, 15, 38, 248, TimeSpan.Zero),
                                  CreatedAt = new DateTimeOffset(2025, 3, 25, 15, 15, 38, 248, TimeSpan.Zero),
                                  WebAccessCode = "null",
                                  EnergySupplier = "8100000000108",
                                  BusinessTransactionDosId = 341696030,
                                  TransactionType = "ENDSUPPLY"
                              }
                        ],
                    },
                    new CommercialRelationEntity
                    {
                        // Id = 3790625001,
                        MeteringPointId = 3790625,
                        EnergySupplier = "5790002424762",
                        StartDate = new DateTimeOffset(2025, 3, 25, 23, 0, 0, 0, TimeSpan.Zero),
                        EndDate = DateTimeOffset.MaxValue,
                        ModifiedAt = new DateTimeOffset(2020, 3, 2, 14, 10, 38, 552, TimeSpan.Zero),
                        ClientId = Guid.NewGuid(),
                        ElectricalHeatingPeriods = [],
                        EnergySupplyPeriods =
                        [
                            new EnergySupplyPeriodEntity
                            {
                                // Id = 3790625001000,
                                CommercialRelationId = 3790625000,
                                ValidFrom = new DateTimeOffset(2025, 3, 25, 23, 0, 0, 0, TimeSpan.Zero),
                                ValidTo = DateTimeOffset.MaxValue,
                                CreatedAt = new DateTimeOffset(2020, 3, 2, 14, 10, 38, 552, TimeSpan.Zero),
                                WebAccessCode = "webAccessCode",
                                EnergySupplier = "5790002424762",
                                BusinessTransactionDosId = 270889573,
                                TransactionType = "MOVEINES",
                            },
                        ],
                    },
                },
                MeteringPointPeriods =
                {
                      new MeteringPointPeriodEntity
                      {
                          // Id = 38277810000,
                          MeteringPointId = 3827781,
                          ValidFrom = new DateTimeOffset(2025, 3, 17, 23, 0, 0, 0, TimeSpan.Zero),
                          ValidTo = new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero),
                          RetiredById = 1,
                          RetiredAt = new DateTimeOffset(2025, 5, 1, 9, 40, 10, 700, TimeSpan.Zero),
                          CreatedAt = new DateTimeOffset(2025, 3, 19, 15, 30, 37, 977, TimeSpan.Zero),
                          ParentIdentification = null,
                          Type = "Consumption",
                          SubType = "Physical",
                          ConnectionState = "New",
                          Resolution = "PT1H",
                          GridAreaCode = "804",
                          OwnedBy = null,
                          ConnectionType = null,
                          DisconnectionType = "ManualDisconnection",
                          Product = "EnergyActive",
                          ProductObligation = null,
                          MeasureUnit = "kWh",
                          AssetType = null,
                          FuelType = null,
                          Capacity = null,
                          PowerLimitKw = 11.0m,
                          PowerLimitA = 16,
                          MeterNumber = "00110048",
                          SettlementGroup = null,
                          ScheduledMeterReadingMonth = null,
                          ExchangeFromGridArea = null,
                          ExchangeToGridArea = null,
                          PowerPlantGsrn = null,
                          SettlementMethod = "FlexSettled",
                          InstallationAddressId = 1,
                          MeteringPointStateId = 224249835,
                          BusinessTransactionDosId = 341693266,
                          TransactionType = "CREATEMP"
                      },
                      new MeteringPointPeriodEntity
                      {
                          // Id = 38277810001,
                          MeteringPointId = 3827781,
                          ValidFrom = new DateTimeOffset(2025, 3, 17, 23, 0, 0, 0, TimeSpan.Zero),
                          ValidTo = new DateTimeOffset(2025, 3, 18, 23, 0, 0, 0, TimeSpan.Zero),
                          RetiredById = null,
                          RetiredAt = null,
                          CreatedAt = new DateTimeOffset(2025, 3, 19, 15, 30, 37, 977, TimeSpan.Zero),
                          ParentIdentification = null,
                          Type = "Consumption",
                          SubType = "Physical",
                          ConnectionState = "New",
                          Resolution = "PT1H",
                          GridAreaCode = "804",
                          OwnedBy = null,
                          ConnectionType = null,
                          DisconnectionType = "ManualDisconnection",
                          Product = "EnergyActive",
                          ProductObligation = null,
                          MeasureUnit = "kWh",
                          AssetType = null,
                          FuelType = null,
                          Capacity = null,
                          PowerLimitKw = 11.0m,
                          PowerLimitA = 16,
                          MeterNumber = "00110048",
                          SettlementGroup = null,
                          ScheduledMeterReadingMonth = null,
                          ExchangeFromGridArea = null,
                          ExchangeToGridArea = null,
                          PowerPlantGsrn = null,
                          SettlementMethod = "FlexSettled",
                          InstallationAddressId = 2,
                          MeteringPointStateId = 224249835,
                          BusinessTransactionDosId = 341693266,
                          TransactionType = "CREATEMP"
                      },
                      new MeteringPointPeriodEntity
                      {
                          // Id = 38277810002,
                          MeteringPointId = 3827781,
                          ValidFrom = new DateTimeOffset(2025, 3, 18, 23, 0, 0, 0, TimeSpan.Zero),
                          ValidTo = DateTimeOffset.MaxValue,
                          RetiredById = 2,
                          RetiredAt = new DateTimeOffset(2025, 5, 1, 9, 40, 10, 700, TimeSpan.Zero),
                          CreatedAt = new DateTimeOffset(2025, 3, 25, 15, 15, 38, 248, TimeSpan.Zero),
                          ParentIdentification = null,
                          Type = "Consumption",
                          SubType = "Physical",
                          ConnectionState = "Connected",
                          Resolution = "PT1H",
                          GridAreaCode = "804",
                          OwnedBy = null,
                          ConnectionType = null,
                          DisconnectionType = "ManualDisconnection",
                          Product = "EnergyActive",
                          ProductObligation = null,
                          MeasureUnit = "kWh",
                          AssetType = null,
                          FuelType = null,
                          Capacity = null,
                          PowerLimitKw = 11.0m,
                          PowerLimitA = 16,
                          MeterNumber = "00110048",
                          SettlementGroup = null,
                          ScheduledMeterReadingMonth = null,
                          ExchangeFromGridArea = null,
                          ExchangeToGridArea = null,
                          PowerPlantGsrn = null,
                          SettlementMethod = "FlexSettled",
                          InstallationAddressId = 3,
                          MeteringPointStateId = 224250418,
                          BusinessTransactionDosId = 341696029,
                          TransactionType = "CONNECTMP"
                      },
                      new MeteringPointPeriodEntity
                      {
                          // Id = 38277810003,
                          MeteringPointId = 3827781,
                          ValidFrom = new DateTimeOffset(2025, 3, 18, 23, 0, 0, 0, TimeSpan.Zero),
                          ValidTo = new DateTimeOffset(2025, 3, 24, 23, 0, 0, 0, TimeSpan.Zero),
                          RetiredById = null,
                          RetiredAt = null,
                          CreatedAt = new DateTimeOffset(2025, 3, 25, 15, 15, 38, 248, TimeSpan.Zero),
                          ParentIdentification = null,
                          Type = "Consumption",
                          SubType = "Physical",
                          ConnectionState = "Connected",
                          Resolution = "PT1H",
                          GridAreaCode = "804",
                          OwnedBy = null,
                          ConnectionType = null,
                          DisconnectionType = "ManualDisconnection",
                          Product = "EnergyActive",
                          ProductObligation = null,
                          MeasureUnit = "kWh",
                          AssetType = null,
                          FuelType = null,
                          Capacity = null,
                          PowerLimitKw = 11.0m,
                          PowerLimitA = 16,
                          MeterNumber = "00110048",
                          SettlementGroup = null,
                          ScheduledMeterReadingMonth = null,
                          ExchangeFromGridArea = null,
                          ExchangeToGridArea = null,
                          PowerPlantGsrn = null,
                          SettlementMethod = "FlexSettled",
                          InstallationAddressId = 4,
                          MeteringPointStateId = 224250418,
                          BusinessTransactionDosId = 341696029,
                          TransactionType = "CONNECTMP"
                      },
                      new MeteringPointPeriodEntity
                      {
                          // Id = 38277810004,
                          MeteringPointId = 3827781,
                          ValidFrom = new DateTimeOffset(2025, 3, 24, 23, 0, 0, 0, TimeSpan.Zero),
                          ValidTo = DateTimeOffset.MaxValue,
                          RetiredById = null,
                          RetiredAt = null,
                          CreatedAt = new DateTimeOffset(2025, 3, 25, 15, 15, 38, 248, TimeSpan.Zero),
                          ParentIdentification = null,
                          Type = "Consumption",
                          SubType = "Physical",
                          ConnectionState = "Disconnected",
                          Resolution = "PT1H",
                          GridAreaCode = "804",
                          OwnedBy = null,
                          ConnectionType = null,
                          DisconnectionType = "ManualDisconnection",
                          Product = "EnergyActive",
                          ProductObligation = null,
                          MeasureUnit = "kWh",
                          AssetType = null,
                          FuelType = null,
                          Capacity = null,
                          PowerLimitKw = 11.0m,
                          PowerLimitA = 16,
                          MeterNumber = "00110048",
                          SettlementGroup = null,
                          ScheduledMeterReadingMonth = null,
                          ExchangeFromGridArea = null,
                          ExchangeToGridArea = null,
                          PowerPlantGsrn = null,
                          SettlementMethod = "FlexSettled",
                          InstallationAddressId = 5,
                          MeteringPointStateId = 224250417,
                          BusinessTransactionDosId = 341696030,
                          TransactionType = "ENDSUPPLY"
                      }
                }
            });

        await context.SaveChangesAsync();
    }
}
