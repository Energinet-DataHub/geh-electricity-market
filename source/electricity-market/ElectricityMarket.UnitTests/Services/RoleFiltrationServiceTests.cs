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
using Energinet.DataHub.ElectricityMarket.Application.Security;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.UnitTests.Common;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Services;

[UnitTest]
public class RoleFiltrationServiceTests
{
    [Fact]
    public void FilterFields_WhenDatahubAdministrator_ReturnsSameMeteringPoint()
    {
        // Arrange
        var tenant = new TenantDto("101", MarketRole.DataHubAdministrator);
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(1, tenant.ActorNumber);

        var target = new RoleFiltrationService();

        // Act
        var result = target.FilterFields(meteringPoint, tenant);

        // Assert
        Assert.Equal(meteringPoint, result);
    }

    [Fact]
    public void FilterFields_WhenEnergySupplier()
    {
        // Arrange
        var tenant = new TenantDto("373", MarketRole.EnergySupplier);
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

        var target = new RoleFiltrationService();

        // Act
        var result = target.FilterFields(meteringPoint, tenant);

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
    public void FilterFields_WhenGridAccessProvider()
    {
        // Arrange
        var tenant = new TenantDto("45", MarketRole.GridAccessProvider);
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

        var target = new RoleFiltrationService();

        // Act
        var result = target.FilterFields(meteringPoint, tenant);

        // Assert
        Assert.NotEqual(meteringPoint, result);
        Assert.NotNull(result);
        Assert.Empty(result.CommercialRelation!.EnergySupplier);
        Assert.Equal(DateTimeOffset.MinValue, result.CommercialRelation!.ActiveEnergySupplyPeriod!.ValidFrom);
        Assert.NotEmpty(result.CommercialRelation!.ActiveEnergySupplyPeriod!.Customers);
        Assert.NotNull(result.Metadata);
        Assert.Equal(tenant.ActorNumber, result.Metadata!.OwnedBy);
    }
}
