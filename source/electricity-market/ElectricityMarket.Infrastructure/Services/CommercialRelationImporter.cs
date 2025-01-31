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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class CommercialRelationImporter : ITransactionImporter
{
    private readonly string[] _validMeteringPointTypes = ["Production", "Consumption"];
    public Task<TransactionImporterResult> ImportAsync(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(meteringPointTransaction);

        if (!_validMeteringPointTypes.Contains(ExternalMeteringPointTypeMapper.Map(meteringPointTransaction.TypeOfMp)))
        {
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Unhandled));
        }

        var latestRelation = meteringPoint.CommercialRelations
            .Where(x => x.StartDate < x.EndDate)
            .MaxBy(x => x.EndDate);

        var latestEnergyPeriod = latestRelation?
            .EnergySupplierPeriods
            .Single(x => x.ValidTo == DateTimeOffset.MaxValue && x.RetiredById is null);

        var newRelation = CreateRelation(meteringPoint, meteringPointTransaction);
        var newEnergyPeriod = CreateEnergyPeriod(meteringPointTransaction);

        // This is the first time we encounter this metering point, so we have no current relations
        if (latestRelation is null)
        {
            newRelation.EnergySupplierPeriods.Add(newEnergyPeriod);
            meteringPoint.CommercialRelations.Add(newRelation);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        if (!string.Equals(meteringPointTransaction.WebAccessCode, latestEnergyPeriod?.WebAccessCode, StringComparison.OrdinalIgnoreCase))
        {
            // MoveIn
            newRelation.EnergySupplierPeriods.Add(newEnergyPeriod);
            meteringPoint.CommercialRelations.Add(newRelation);
            latestRelation.EndDate = meteringPointTransaction.ValidFrom;
            latestRelation.ModifiedAt = DateTimeOffset.UtcNow;
        }
        else if (!string.Equals(meteringPointTransaction.EnergySupplier, latestRelation.EnergySupplier, StringComparison.OrdinalIgnoreCase))
        {
            // ChangeSupplier
            newRelation.EnergySupplierPeriods.Add(newEnergyPeriod);
            newRelation.CustomerId = latestRelation.CustomerId;
            meteringPoint.CommercialRelations.Add(newRelation);
            latestRelation.EndDate = meteringPointTransaction.ValidFrom;
            latestRelation.ModifiedAt = DateTimeOffset.UtcNow;
        }
        else if (latestEnergyPeriod is not null && IsLatestPeriodRetired(latestEnergyPeriod, newEnergyPeriod))
        {
            var copy = CreateEnergyPeriodCopy(latestEnergyPeriod);
            copy.ValidTo = meteringPointTransaction.ValidFrom;
            latestEnergyPeriod.RetiredBy = copy;
            latestEnergyPeriod.RetiredAt = DateTimeOffset.UtcNow;
            latestRelation.EnergySupplierPeriods.Add(copy);
            latestRelation.EnergySupplierPeriods.Add(newEnergyPeriod);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Unhandled));
    }

    private static bool IsLatestPeriodRetired(EnergySupplyPeriodEntity latestEnergyPeriod, EnergySupplyPeriodEntity incomingEnergyPeriod)
    {
        return latestEnergyPeriod.ValidTo == DateTimeOffset.MaxValue && incomingEnergyPeriod.ValidFrom >= latestEnergyPeriod.ValidFrom;
    }

    private static CommercialRelationEntity CreateRelation(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        return new CommercialRelationEntity
        {
            MeteringPointId = meteringPoint.Id,
            StartDate = meteringPointTransaction.ValidFrom,
            EndDate = meteringPointTransaction.ValidTo,
            EnergySupplier = meteringPointTransaction.EnergySupplier,
            CustomerId = Guid.NewGuid().ToString(),
            ModifiedAt = DateTimeOffset.UtcNow,
            EnergySupplierPeriods = new List<EnergySupplyPeriodEntity>(),
        };
    }

    private static EnergySupplyPeriodEntity CreateEnergyPeriod(MeteringPointTransaction meteringPointTransaction)
    {
        return new EnergySupplyPeriodEntity
        {
            ValidFrom = meteringPointTransaction.ValidFrom,
            ValidTo = meteringPointTransaction.ValidTo,
            EnergySupplier = meteringPointTransaction.EnergySupplier,
            WebAccessCode = meteringPointTransaction.WebAccessCode,
            BusinessTransactionDosId = meteringPointTransaction.BusinessTransactionDosId,
        };
    }

    private static EnergySupplyPeriodEntity CreateEnergyPeriodCopy(EnergySupplyPeriodEntity energyPeriodEntity)
    {
        return new EnergySupplyPeriodEntity
        {
            ValidFrom = energyPeriodEntity.ValidFrom,
            ValidTo = energyPeriodEntity.ValidTo,
            EnergySupplier = energyPeriodEntity.EnergySupplier,
            WebAccessCode = energyPeriodEntity.WebAccessCode,
            BusinessTransactionDosId = energyPeriodEntity.BusinessTransactionDosId,
        };
    }
}
