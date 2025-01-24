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

using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;

public interface IElectricityMarketDatabaseContext
{
    DatabaseFacade Database { get; }
    DbSet<MeteringPointEntity> MeteringPoints { get; }
    DbSet<MeteringPointPeriodEntity> MeteringPointPeriods { get; }
    DbSet<CommercialRelationEntity> CommercialRelations { get; }
    DbSet<ImportStateEntity> ImportStates { get; }
    DbSet<SpeedTestImportEntity> SpeedTestImportEntities { get; }
    DbSet<SpeedTestGoldEntity> SpeedTestGoldEntities { get; }
    DbSet<QuarantinedMeteringPointEntity> QuarantinedMeteringPointEntities { get; }
    DbSet<QuarantinedMeteringPointTransactionEntity> QuarantinedMeteringPointTransactionEntities { get; }

    Task<int> SaveChangesAsync();
}
