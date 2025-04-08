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

public class MeteringPointIntegrationRepositoryTests : IClassFixture<ElectricityMarketDatabaseFixture>, IAsyncLifetime
{
    private readonly ElectricityMarketDatabaseFixture _fixture;

    public MeteringPointIntegrationRepositoryTests(ElectricityMarketDatabaseFixture fixture)
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
                new DateTimeOffset(2023, 10, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero))
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

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

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
                //Id = 37906250000,
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
                //Id = 3790625,
                Version = new DateTimeOffset(2025, 4, 7, 8, 38, 13, 0, TimeSpan.Zero),
                CommercialRelations =
                {
                    new CommercialRelationEntity
                    {
                        //Id = 3790625000,
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
                                //Id = 3790625000000,
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
                        //Id = 3790625001,
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
                                //Id = 3790625001000,
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
                        //Id = 3790625002,
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
                                //Id = 3790625002000,
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
                        //Id = 3790625003,
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
                                //Id = 3790625003000,
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
                                //Id = 3790625003001,
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
                        //Id = 37906250000,
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

        await context.InstallationAddresses.AddAsync(
            new InstallationAddressEntity
            {
                //Id = 516430002,
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
                //Id = 51643,
                Version = new DateTimeOffset(2025, 4, 7, 8, 18, 3, 0, TimeSpan.Zero),
                CommercialRelations =
                {
                    new CommercialRelationEntity
                    {
                        //Id = 51643000,
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
                                //Id = 51643000000,
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
                        //Id = 516430000,
                        MeteringPointId = 51643,
                        ValidFrom = new DateTimeOffset(2017, 1, 19, 23, 0, 0, 0, TimeSpan.Zero),
                        ValidTo = DateTimeOffset.MaxValue,
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
                },
            });

        await context.SaveChangesAsync();
    }
}
