﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Common;

public static class TestCommercialRelationPreparationHelper
{
    public static Task<CommercialRelationEntity> PrepareCommercialRelationAsync(
        this ElectricityMarketDbUpDatabaseFixture fixture)
    {
        return PrepareCommercialRelationAsync(
            fixture,
            TestPreparationEntities.ValidMeteringPoint,
            TestPreparationEntities.ValidCommercialRelation,
            TestPreparationEntities.ValidEnergySupplyPeriodEntity);
    }

    public static async Task<CommercialRelationEntity> PrepareCommercialRelationAsync(
        this ElectricityMarketDbUpDatabaseFixture fixture,
        MeteringPointEntity meteringPointEntity,
        CommercialRelationEntity commercialRelationEntity,
        EnergySupplyPeriodEntity energySupplyPeriodEntity)
    {
        await using var context = fixture.DbUpDatabaseManager.CreateDbContext();

        if (meteringPointEntity.Id == 0)
        {
            await context.MeteringPoints.AddAsync(meteringPointEntity);
            await context.SaveChangesAsync();
        }

        commercialRelationEntity.MeteringPointId = meteringPointEntity.Id;
        commercialRelationEntity.EnergySupplyPeriods.Add(energySupplyPeriodEntity);

        await context.CommercialRelations.AddAsync(commercialRelationEntity);
        await context.SaveChangesAsync();

        return commercialRelationEntity;
    }
}
