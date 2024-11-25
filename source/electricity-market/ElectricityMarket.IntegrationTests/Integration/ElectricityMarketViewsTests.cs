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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Entities;
using Energinet.DataHub.ElectricityMarket.Integration;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Extensions;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Integration;

[Collection(nameof(IntegrationTestIntegrationCollectionFixture))]
public sealed class ElectricityMarketViewsTests
{
    private readonly ElectricityMarketIntegrationFixture _fixture;

    public ElectricityMarketViewsTests(
        ElectricityMarketIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MeteringPointChangesAsync_FilterSupplied_AdheresTo()
    {
        // arrange
        await _fixture.InsertAsync(Records('1'));
        var serviceProvider = _fixture.BuildServiceProvider();
        var target = serviceProvider.GetRequiredService<IElectricityMarketViews>();

        var actualRecords = new List<MeteringPointChange>();
        await foreach (var view in target.GetMeteringPointChangesAsync(new MeteringPointIdentification("100000000000000005")))
        {
            actualRecords.Add(view);
        }

        // assert
        Assert.Single(actualRecords);
        Assert.Equal("100000000000000005", actualRecords.Single().Identification.Value);
    }

    [Fact]
    public async Task MeteringPointEnergySuppliersAsync_FilterSupplied_AdheresTo()
    {
        // arrange
        await _fixture.InsertAsync(Records('2'));
        var serviceProvider = _fixture.BuildServiceProvider();
        var target = serviceProvider.GetRequiredService<IElectricityMarketViews>();

        var actualRecords = new List<MeteringPointEnergySupplier>();
        await foreach (var view in target.GetMeteringPointEnergySuppliersAsync(new MeteringPointIdentification("200000000000000006")))
        {
            actualRecords.Add(view);
        }

        // assert
        Assert.Single(actualRecords);
        Assert.Equal("200000000000000006", actualRecords.Single().Identification.Value);
    }

    private static IEnumerable<(MeteringPointEntity MeteringPointEntity, MeteringPointPeriodEntity MeteringPointPeriodEntity, CommercialRelationEntity CommercialRelationEntity)> Records(char prefix)
    {
        for (var i = 0; i < 10; ++i)
        {
            yield return (
                MeteringPointEntity: MeteringPointEntityHelper.Create(identification: new MeteringPointIdentification(prefix + "0000000000000" + (i + 1).ToString().PadLeft(4, '0'))),
                MeteringPointPeriodEntity: MeteringPointPeriodEntityHelper.Create(),
                CommercialRelationEntity: CommercialRelationEntityHelper.Create());
        }
    }
}
