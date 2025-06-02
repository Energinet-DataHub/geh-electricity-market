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
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Security;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.UnitTests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Services;

[UnitTest]
public class RoleFiltrationServiceTests
{
    private readonly Mock<IMeteringPointDelegationRepository> _meteringPointDelegationRepository;

    public RoleFiltrationServiceTests()
    {
        _meteringPointDelegationRepository = new Mock<IMeteringPointDelegationRepository>();
    }

    [Fact]
    public async Task FilterFields_WhenDatahubAdministrator_ReturnsSameMeteringPointAsync()
    {
        // Arrange
        var tenant = new TenantDto("101", EicFunction.DataHubAdministrator);
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(1, tenant.ActorNumber);

        var target = new RoleFiltrationService(_meteringPointDelegationRepository.Object);

        // Act
        var result = await target.FilterFieldsAsync(meteringPoint, tenant);

        // Assert
        Assert.Equal(meteringPoint, result);
    }

    [Fact]
    public async Task FilterFields_WhenEnergySupplier_ReturnsFilteredMeteringPointAsync()
    {
        // Arrange
        var tenant = new TenantDto("373", EicFunction.EnergySupplier);
        var mockedMeteringPointMetadata = MockedMeteringPointObjects.GetMockedMeteringPointMetadata(11, tenant.ActorNumber);
        var mockedMeteringPointMetadataNotInTenant = MockedMeteringPointObjects.GetMockedMeteringPointMetadata(12, "123");
        var mockedCommercialRelation = MockedMeteringPointObjects.GetMockedCommercialRelation(13, tenant.ActorNumber);
        var mockedCommercialRelationNotInTenant = MockedMeteringPointObjects.GetMockedCommercialRelation(14, "123");
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(
            1,
            tenant.ActorNumber,
            mockedMeteringPointMetadata,
            [mockedMeteringPointMetadata, mockedMeteringPointMetadataNotInTenant],
            mockedCommercialRelation,
            [mockedCommercialRelation, mockedCommercialRelationNotInTenant]);

        var target = new RoleFiltrationService(_meteringPointDelegationRepository.Object);

        // Act
        var result = await target.FilterFieldsAsync(meteringPoint, tenant);

        var ownedCommercialRelations = result?.CommercialRelationTimeline?.FirstOrDefault(x => x.Id == 13);
        var notOwnedCommercialRelations = result?.CommercialRelationTimeline?.FirstOrDefault(x => x.Id == 14);

        // Assert
        Assert.Empty(notOwnedCommercialRelations!.EnergySupplier);
        Assert.NotEmpty(ownedCommercialRelations!.EnergySupplier);
        Assert.NotEmpty(notOwnedCommercialRelations!.ActiveEnergySupplyPeriod!.Customers!.FirstOrDefault()!.Cvr!);
        Assert.NotNull(ownedCommercialRelations!.ActiveEnergySupplyPeriod!.Customers.First().LegalContact);
        Assert.NotNull(ownedCommercialRelations?.ActiveEnergySupplyPeriod.Customers.First().TechnicalContact);
        Assert.Null(notOwnedCommercialRelations?.ActiveEnergySupplyPeriod.Customers.First().LegalContact);
        Assert.Null(notOwnedCommercialRelations?.ActiveEnergySupplyPeriod.Customers.First().TechnicalContact);
    }

    [Fact]
    public async Task FilterFields_WhenGridAccessProvider_ReturnsFilteredMeteringPointAsync()
    {
        // Arrange
        var tenant = new TenantDto("45", EicFunction.GridAccessProvider);
        var mockedMeteringPointMetadata = MockedMeteringPointObjects.GetMockedMeteringPointMetadata(11, tenant.ActorNumber);
        var mockedMeteringPointMetadataNotInTenant = MockedMeteringPointObjects.GetMockedMeteringPointMetadata(12, "123");
        var mockedCommercialRelation = MockedMeteringPointObjects.GetMockedCommercialRelation(13, tenant.ActorNumber);
        var mockedCommercialRelationNotInTenant = MockedMeteringPointObjects.GetMockedCommercialRelation(14, "123");
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(
            1,
            tenant.ActorNumber,
            mockedMeteringPointMetadata,
            [mockedMeteringPointMetadata, mockedMeteringPointMetadataNotInTenant],
            mockedCommercialRelation,
            [mockedCommercialRelation, mockedCommercialRelationNotInTenant]);

        var target = new RoleFiltrationService(_meteringPointDelegationRepository.Object);

        // Act
        var result = await target.FilterFieldsAsync(meteringPoint, tenant);

        // Assert
        Assert.NotEqual(meteringPoint, result);
        Assert.NotNull(result);
        Assert.Empty(result.CommercialRelation!.EnergySupplier);
        Assert.Equal(DateTimeOffset.MinValue, result.CommercialRelation.StartDate);
        Assert.NotEmpty(result.CommercialRelation!.ActiveEnergySupplyPeriod!.Customers);
        Assert.NotNull(result.Metadata);
        Assert.Equal(tenant.ActorNumber, result.Metadata!.OwnedBy);
    }

    [Fact]
    public async Task FilterFields_WhenDelegated_ReturnsFilteredMeteringPointAsync()
    {
        // Arrange
        var tenant = new TenantDto("45", EicFunction.Delegated);
        var mockedMeteringPointMetadata = MockedMeteringPointObjects.GetMockedMeteringPointMetadata(11, tenant.ActorNumber);
        var mockedMeteringPointMetadataNotInTenant = MockedMeteringPointObjects.GetMockedMeteringPointMetadata(12, "123");
        var mockedCommercialRelation = MockedMeteringPointObjects.GetMockedCommercialRelation(13, tenant.ActorNumber);
        var mockedCommercialRelationNotInTenant = MockedMeteringPointObjects.GetMockedCommercialRelation(14, "123");
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(
            1,
            tenant.ActorNumber,
            mockedMeteringPointMetadata,
            [mockedMeteringPointMetadata, mockedMeteringPointMetadataNotInTenant],
            mockedCommercialRelation,
            [mockedCommercialRelation, mockedCommercialRelationNotInTenant]);

        _meteringPointDelegationRepository
            .Setup(x => x.GetDelegationsAsync(It.IsAny<MeteringPointIdentification>()))
            .ReturnsAsync(MockedMeteringPointObjects.GetMockedDelegations(tenant.ActorNumber));

        var target = new RoleFiltrationService(_meteringPointDelegationRepository.Object);

        // Act
        var result = await target.FilterFieldsAsync(meteringPoint, tenant);

        // Assert
        Assert.NotNull(result);
    }
}
