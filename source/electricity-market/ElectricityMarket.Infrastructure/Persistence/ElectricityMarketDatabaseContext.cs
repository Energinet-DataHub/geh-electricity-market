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

using System;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model.MarketParticipant;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration;
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
    public DbSet<InstallationAddressEntity> InstallationAddresses { get; private set; } = null!;
    public DbSet<MeteringPointPeriodEntity> MeteringPointPeriods { get; private set; } = null!;
    public DbSet<CommercialRelationEntity> CommercialRelations { get; private set; } = null!;
    public DbSet<ElectricalHeatingPeriodEntity> ElectricalHeatingPeriods { get; private set; } = null!;
    public DbSet<EnergySupplyPeriodEntity> EnergySupplyPeriods { get; private set; } = null!;
    public DbSet<ContactAddressEntity> ContactAddresses { get; private set; } = null!;
    public DbSet<ContactEntity> Contacts { get; private set; } = null!;

    public DbSet<QuarantinedMeteringPointEntity> QuarantinedMeteringPointEntities { get; private set; } = null!;
    public DbSet<ImportedTransactionEntity> ImportedTransactions { get; private set; } = null!;
    public DbSet<ImportStateEntity> ImportStates { get; private set; } = null!;

    public DbSet<GridAreaEntity> GridAreas { get; private set; }
    public DbSet<ActorEntity> Actors { get; private set; } = null!;
    public DbSet<MarketRoleEntity> MarketRoles { get; private set; } = null!;
    public DbSet<MarketRoleGridAreaEntity> MarketRoleGridAreas { get; private set; } = null!;
    public DbSet<SyncJobsEntity> SyncJobs { get; private set; } = null!;

    public DbSet<ProcessDelegationEntity> ProcessDelegations { get; private set; } = null!;
    public DbSet<DelegationPeriodEntity> DelegationPeriods { get; private set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("electricitymarket");
        modelBuilder.ApplyConfiguration(new ImportStateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MeteringPointEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InstallationAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MeteringPointPeriodEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CommercialRelationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ElectricalHeatingPeriodEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EnergySupplyPeriodEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContactAddressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContactEntityConfiguration());

        modelBuilder.ApplyConfiguration(new ImportedTransactionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QuarantinedMeteringPointEntityConfiguration());

        modelBuilder.ApplyConfiguration(new GridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarketRoleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarketRoleGridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SyncJobsEntityConfiguration());

        modelBuilder.ApplyConfiguration(new ProcessDelegationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DelegationPeriodEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
