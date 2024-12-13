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

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Integration;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Common;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Integration;

[Collection(nameof(IntegrationTestIntegrationCollectionFixture))]
public sealed class ElectricityMarketViewsTests
{
    private readonly ElectricityMarketIntegrationFixture _fixture;

    public ElectricityMarketViewsTests(ElectricityMarketIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetMeteringPointMasterDataAsync_FilterSupplied_AdheresToFilter()
    {
        // Arrange
        foreach (var meteringPointEntity in CreateRecords('1'))
        {
            var mp = await _fixture.PrepareMeteringPointAsync(meteringPointEntity);
            await _fixture.PrepareMeteringPointPeriodAsync(mp, TestPreparationEntities.ValidMeteringPointPeriod);
        }

        await using var scope = _fixture.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IElectricityMarketViews>();

        // Act
        var actual = await target.GetMeteringPointMasterDataAsync(
            new MeteringPointIdentification("100000000000000005"),
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        Assert.NotNull(actual);
        Assert.Equal("100000000000000005", actual.Identification.Value);
    }

    [Fact]
    public async Task MeteringPointEnergySuppliersAsync_FilterSupplied_AdheresToFilter()
    {
        // Arrange
        foreach (var meteringPointEntity in CreateRecords('2'))
        {
            var mp = await _fixture.PrepareMeteringPointAsync(meteringPointEntity);
            await _fixture.PrepareMeteringPointPeriodAsync(mp, TestPreparationEntities.ValidMeteringPointPeriod);
            await _fixture.PrepareCommercialRelationAsync(mp, TestPreparationEntities.ValidCommercialRelation);
        }

        await using var scope = _fixture.BeginScope();
        var target = scope.ServiceProvider.GetRequiredService<IElectricityMarketViews>();

        // Act
        var actual = await target.GetMeteringPointEnergySupplierAsync(
            new MeteringPointIdentification("200000000000000006"),
            SystemClock.Instance.GetCurrentInstant());

        // Assert
        Assert.NotNull(actual);
        Assert.Equal("200000000000000006", actual.Identification.Value);
    }

    private static IEnumerable<MeteringPointEntity> CreateRecords(char prefix)
    {
        for (var i = 0; i < 10; ++i)
        {
            var id = prefix + "0000000000000" + (i + 1).ToString(CultureInfo.InvariantCulture).PadLeft(4, '0');
            yield return TestPreparationEntities.ValidMeteringPoint.Patch(mp => mp.Identification = id);
        }
    }
}
