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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model.MarketParticipant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.EntityConfiguration
{
    public class BalanceResponsibilityRelationConfiguration : IEntityTypeConfiguration<BalanceResponsibilityRelationEntity>
    {
        public void Configure(EntityTypeBuilder<BalanceResponsibilityRelationEntity> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.ToTable("BalanceResponsibilityRelation", "dbo");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("Id")
                .IsRequired();

            builder.Property(e => e.EnergySupplierId)
                .HasColumnName("EnergySupplierId")
                .IsRequired();

            builder.Property(e => e.BalanceResponsiblePartyId)
                .HasColumnName("BalanceResponsiblePartyId")
                .IsRequired();

            builder.Property(e => e.GridAreaId)
                .HasColumnName("GridAreaId")
                .IsRequired();

            builder.Property(e => e.MeteringPointType)
                .HasColumnName("MeteringPointType")
                .IsRequired();

            builder.Property(e => e.ValidFrom)
                .HasColumnName("ValidFrom")
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();

            builder.Property(e => e.ValidTo)
                .HasColumnName("ValidTo")
                .HasColumnType("datetimeoffset(7)")
                .IsRequired(false);

            builder.Property(e => e.ValidToAssignedAt)
                .HasColumnName("ValidToAssignedAt")
                .HasColumnType("datetimeoffset(7)")
                .IsRequired(false);
        }
    }
}
