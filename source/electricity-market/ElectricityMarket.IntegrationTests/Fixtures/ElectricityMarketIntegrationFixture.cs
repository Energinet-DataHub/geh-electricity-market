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
using Energinet.DataHub.ElectricityMarket.Integration.Extensions.DependencyInjection;
using Energinet.DataHub.ElectricityMarket.Integration.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;

public sealed class ElectricityMarketIntegrationFixture : IAsyncLifetime
{
    internal ElectricityMarketIntegrationDatabaseManager DatabaseManager { get; } = new();

    public Task InitializeAsync()
    {
        return DatabaseManager.CreateDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return DatabaseManager.DeleteDatabaseAsync();
    }

    public IServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddSingleton(BuildConfiguration(DatabaseManager.ConnectionString))
            .AddElectricityMarketModule()
            .BuildServiceProvider();
    }

    public async Task InsertAsync(IEnumerable<(
        MeteringPointEntity MeteringPointEntity,
        MeteringPointPeriodEntity MeteringPointPeriodEntity,
        CommercialRelationEntity CommercialRelationEntity)> records)
    {
        await using var context = DatabaseManager.CreateDbContext();

        foreach (var (meteringPointEntity, meteringPointPeriodEntity, commercialRelationEntity) in records)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 INSERT INTO MeteringPoint(Identification)
                 VALUES({meteringPointEntity.Identification});

                 DECLARE @id bigint = SCOPE_IDENTITY();

                 INSERT INTO MeteringPointPeriod(MeteringPointId, ValidFrom, ValidTo, GridAreaCode, GridAccessProvider, ConnectionState, SubType, Resolution, Unit, ProductId)
                 VALUES(@id, {meteringPointPeriodEntity.ValidFrom.ToDateTimeOffset()}, {meteringPointPeriodEntity.ValidTo.ToDateTimeOffset()}, {meteringPointPeriodEntity.GridAreaCode}, {meteringPointPeriodEntity.GridAccessProvider}, {meteringPointPeriodEntity.ConnectionState}, {meteringPointPeriodEntity.SubType}, {meteringPointPeriodEntity.Resolution}, {meteringPointPeriodEntity.Unit}, {meteringPointPeriodEntity.ProductId})

                 INSERT INTO CommercialRelation(MeteringPointId, EnergySupplier, StartDate, EndDate)
                 VALUES(@id, {commercialRelationEntity.EnergySupplier}, {commercialRelationEntity.StartDate.ToDateTimeOffset()}, {commercialRelationEntity.EndDate.ToDateTimeOffset()})
                 """);
        }
    }

    private static IConfiguration BuildConfiguration(string connectionString)
    {
        return new ConfigurationBuilder().AddInMemoryCollection([
            new KeyValuePair<string, string?>($"{nameof(DatabaseOptions)}:{nameof(DatabaseOptions.ConnectionString)}", connectionString),
        ]).Build();
    }
}
