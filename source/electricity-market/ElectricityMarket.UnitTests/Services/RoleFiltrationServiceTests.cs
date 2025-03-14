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
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(1);
        var tenant = new TenantDto("101", MarketRole.DataHubAdministrator.ToString());

        var target = new RoleFiltrationService();

        // Act
        var result = target.FilterFields(meteringPoint, tenant);

        // Assert
        Assert.Equal(meteringPoint, result);
    }

    [Fact]
    public void FilterFields_WhenEnergySupplierWithOwnMP_ReturnsSameMeteringPoint()
    {
        // Arrange
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(
            1,
            "373",
            MockedMeteringPointObjects.GetMockedMeteringPointMetadata(11),
            [],
            MockedMeteringPointObjects.GetMockedCommercialRelation(12, "373"),
            []);
        var tenant = new TenantDto("373", MarketRole.EnergySupplier.ToString());

        var target = new RoleFiltrationService();

        // Act
        var result = target.FilterFields(meteringPoint, tenant);

        // Assert
        Assert.Equal(meteringPoint, result);
    }

    [Fact]
    public void FilterFields_WhenEnergySupplierWithForeignMP_ReturnsNoCustomerNoEnergySupplierInfo()
    {
        // Arrange
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(
            1,
            "373",
            MockedMeteringPointObjects.GetMockedMeteringPointMetadata(11),
            [],
            MockedMeteringPointObjects.GetMockedCommercialRelation(12, "373"),
            []);

        var tenant = new TenantDto("45", MarketRole.EnergySupplier.ToString());

        var target = new RoleFiltrationService();

        // Act
        var result = target.FilterFields(meteringPoint, tenant);

        // Assert
        Assert.NotEqual(meteringPoint, result);
        Assert.NotNull(result);
        Assert.Empty(result.CommercialRelation!.EnergySupplier);
        Assert.Equal(DateTimeOffset.MinValue, result.CommercialRelation!.ActiveEnergySupplyPeriod!.ValidFrom);
        Assert.Equal(1, result.CommercialRelation!.ActiveEnergySupplyPeriod!.Customers.Count(c => c.Cvr != null));
        Assert.Equal(0, result.CommercialRelation!.ActiveEnergySupplyPeriod!.Customers.Count(c => c.Cvr == null));
    }

    [Fact]
    public void FilterFields_WhenGridAccessProvider_ReturnsNoEnergySupplierInfo()
    {
        // Arrange
        var meteringPoint = MockedMeteringPointObjects.GetMockedMeteringPoint(
            1,
            "373",
            MockedMeteringPointObjects.GetMockedMeteringPointMetadata(11),
            [],
            MockedMeteringPointObjects.GetMockedCommercialRelation(12, "373"),
            []);

        var tenant = new TenantDto("45", MarketRole.GridAccessProvider.ToString());

        var target = new RoleFiltrationService();

        // Act
        var result = target.FilterFields(meteringPoint, tenant);

        // Assert
        Assert.NotEqual(meteringPoint, result);
        Assert.NotNull(result);
        Assert.Empty(result.CommercialRelation!.EnergySupplier);
        Assert.Equal(DateTimeOffset.MinValue, result.CommercialRelation!.ActiveEnergySupplyPeriod!.ValidFrom);
        Assert.NotEmpty(result.CommercialRelation!.ActiveEnergySupplyPeriod!.Customers);
    }
}
