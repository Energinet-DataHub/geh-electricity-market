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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;

public class ElectricityMarketDatabaseContext : DbContext
{
    public ElectricityMarketDatabaseContext(DbContextOptions<ElectricityMarketDatabaseContext> options)
        : base(options)
    {
    }

    public ElectricityMarketDatabaseContext()
    {
    }

    public DbSet<MeteringPointEntity> MeteringPoints { get; private set; } = null!;
    public DbSet<MeteringPointPeriodEntity> MeteringPointPeriods { get; private set; } = null!;
    public DbSet<CommercialRelationEntity> CommercialRelations { get; private set; } = null!;
    public DbSet<EnergySupplyPeriodEntity> EnergySupplyPeriods { get; private set; } = null!;
    public DbSet<ImportStateEntity> ImportStates { get; private set; } = null!;
    public DbSet<QuarantinedMeteringPointEntity> QuarantinedMeteringPointEntities { get; private set; } = null!;
    public DbSet<QuarantinedMeteringPointTransactionEntity> QuarantinedMeteringPointTransactionEntities { get; private set; } = null!;
    public DbSet<ImportedTransactionEntity> ImportedTransactions { get; private set; } = null!;

    public DbSet<SpeedTestImportEntity> SpeedTestImportEntities { get; private set; } = null!;

    public Task<int> SaveChangesAsync() => base.SaveChangesAsync();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        modelBuilder.HasDefaultSchema("electricitymarket");
        modelBuilder.ApplyConfiguration(new ImportStateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MeteringPointEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MeteringPointPeriodEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CommercialRelationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SpeedTestImportEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ImportedTransactionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QuarantinedMeteringPointEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QuarantinedMeteringPointTransactionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EnergySupplyPeriodEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
