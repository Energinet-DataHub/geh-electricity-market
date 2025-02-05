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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.EntityConfiguration;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model.MarketParticipant;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;

public class MarketParticipantDatabaseContext : DbContext, IMarketParticipantDatabaseContext
{
    public MarketParticipantDatabaseContext(DbContextOptions<MarketParticipantDatabaseContext> options)
        : base(options)
    {
    }

    public MarketParticipantDatabaseContext()
    {
    }

    public DbSet<DomainEventEntity> DomainEvents { get; private set; }
    public DbSet<GridAreaEntity> GridAreas { get; private set; }
    public DbSet<ActorEntity> Actors { get; private set; } = null!;
    public DbSet<ProcessDelegationEntity> ProcessDelegations { get; private set; } = null!;
    public DbSet<DelegationPeriodEntity> DelegationPeriods { get; private set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        modelBuilder.ApplyConfiguration(new DomainEventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessDelegationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DelegationPeriodEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
