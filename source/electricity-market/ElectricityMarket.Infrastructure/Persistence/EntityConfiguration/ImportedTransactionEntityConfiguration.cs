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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.EntityConfiguration;

public sealed class ImportedTransactionEntityConfiguration : IEntityTypeConfiguration<ImportedTransactionEntity>
{
    public void Configure(EntityTypeBuilder<ImportedTransactionEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        builder.ToTable("GoldenImport");
        builder.HasKey(t => new { t.metering_point_id, t.btd_trans_doss_id, t.metering_point_state_id, t.transaction_type });
        builder.Property(t => t.power_limit_kw).HasPrecision(11, 1);
    }
}
