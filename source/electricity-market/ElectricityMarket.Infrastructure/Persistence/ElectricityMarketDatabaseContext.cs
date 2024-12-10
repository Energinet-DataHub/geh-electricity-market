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
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Entities;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;

public class ElectricityMarketDatabaseContext : DbContext, IElectricityMarketDatabaseContext
{
    public ElectricityMarketDatabaseContext(DbContextOptions<ElectricityMarketDatabaseContext> options)
        : base(options)
    {
    }

    public ElectricityMarketDatabaseContext()
    {
    }

    public DbSet<GridAreaEntity> GridAreas { get; private set; } = null!;
    public DbSet<MeteringPointEntity> MeteringPoints { get; private set; } = null!;
    public DbSet<MeteringPointPeriodEntity> MeteringPointPeriods { get; private set; } = null!;
    public DbSet<CommercialRelationEntity> CommercialRelations { get; private set; } = null!;

    public Task<int> SaveChangesAsync() => base.SaveChangesAsync();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        modelBuilder.ApplyConfiguration(new GridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MeteringPointEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MeteringPointPeriodEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CommercialRelationEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
