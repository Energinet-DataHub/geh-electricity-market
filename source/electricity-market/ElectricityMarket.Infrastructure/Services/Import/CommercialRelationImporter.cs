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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class CommercialRelationImporter : ITransactionImporter
{
    private readonly string[] _validMeteringPointTypes = ["Production", "Consumption"];

    public Task<TransactionImporterResult> ImportAsync(MeteringPointEntity meteringPoint, ImportedTransactionEntity importedTransactionEntity)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(importedTransactionEntity);

        if (!_validMeteringPointTypes.Contains(ExternalMeteringPointTypeMapper.Map(importedTransactionEntity.type_of_mp.TrimEnd())))
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));

        var latestRelation = meteringPoint.CommercialRelations
            .Where(x => x.StartDate < x.EndDate)
            .MaxBy(x => x.EndDate);

        var latestEnergyPeriod = latestRelation?
            .EnergySupplyPeriods
            .Single(x => x.ValidTo == DateTimeOffset.MaxValue && x.RetiredBy is null);

        var newRelation = CreateCommercialRelation(meteringPoint, importedTransactionEntity);
        var newEnergyPeriod = CreateEnergySupplyPeriod(importedTransactionEntity);

        // This is the first time we encounter this metering point, so we have no current relations
        if (latestRelation is null)
        {
            newRelation.EnergySupplyPeriods.Add(newEnergyPeriod);
            meteringPoint.CommercialRelations.Add(newRelation);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        if (!string.Equals(importedTransactionEntity.web_access_code?.TrimEnd(), latestEnergyPeriod?.WebAccessCode, StringComparison.OrdinalIgnoreCase))
        {
            // MoveIn
            newRelation.EnergySupplyPeriods.Add(newEnergyPeriod);
            meteringPoint.CommercialRelations.Add(newRelation);
            latestRelation.EndDate = importedTransactionEntity.valid_from_date;
            latestRelation.ModifiedAt = DateTimeOffset.UtcNow;
        }
        else if (!string.Equals(importedTransactionEntity.balance_supplier_id, latestRelation.EnergySupplier, StringComparison.OrdinalIgnoreCase))
        {
            // ChangeSupplier
            newRelation.EnergySupplyPeriods.Add(newEnergyPeriod);
            newRelation.CustomerId = latestRelation.CustomerId;
            meteringPoint.CommercialRelations.Add(newRelation);
            latestRelation.EndDate = importedTransactionEntity.valid_from_date;
            latestRelation.ModifiedAt = DateTimeOffset.UtcNow;
        }
        else if (latestEnergyPeriod is not null && IsLatestPeriodRetired(latestEnergyPeriod, newEnergyPeriod))
        {
            var copy = CreateEnergyPeriodCopy(latestEnergyPeriod);
            copy.ValidTo = importedTransactionEntity.valid_from_date;
            latestEnergyPeriod.RetiredBy = copy;
            latestEnergyPeriod.RetiredAt = DateTimeOffset.UtcNow;
            latestRelation.EnergySupplyPeriods.Add(copy);
            latestRelation.EnergySupplyPeriods.Add(newEnergyPeriod);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Unhandled));
    }

    private static bool IsLatestPeriodRetired(EnergySupplyPeriodEntity latestEnergyPeriod, EnergySupplyPeriodEntity incomingEnergyPeriod)
    {
        return latestEnergyPeriod.ValidTo == DateTimeOffset.MaxValue && incomingEnergyPeriod.ValidFrom >= latestEnergyPeriod.ValidFrom;
    }

    private static CommercialRelationEntity CreateCommercialRelation(MeteringPointEntity meteringPoint, ImportedTransactionEntity importedTransactionEntity)
    {
        return new CommercialRelationEntity
        {
            MeteringPointId = meteringPoint.Id,
            StartDate = importedTransactionEntity.valid_from_date,
            EndDate = DateTimeOffset.MaxValue,
            EnergySupplier = importedTransactionEntity.balance_supplier_id ?? "NULL",
            CustomerId = Guid.NewGuid().ToString(),
            ModifiedAt = DateTimeOffset.UtcNow,
            EnergySupplyPeriods = new List<EnergySupplyPeriodEntity>(),
        };
    }

    private static EnergySupplyPeriodEntity CreateEnergySupplyPeriod(ImportedTransactionEntity importedTransactionEntity)
    {
        return new EnergySupplyPeriodEntity
        {
            ValidFrom = importedTransactionEntity.valid_from_date,
            ValidTo = DateTimeOffset.MaxValue,
            CreatedAt = DateTimeOffset.UtcNow,
            EnergySupplier = importedTransactionEntity.balance_supplier_id ?? "NULL",
            WebAccessCode = importedTransactionEntity.web_access_code ?? "NULL",
            BusinessTransactionDosId = importedTransactionEntity.btd_business_trans_doss_id,
        };
    }

    private static EnergySupplyPeriodEntity CreateEnergyPeriodCopy(EnergySupplyPeriodEntity energyPeriodEntity)
    {
        return new EnergySupplyPeriodEntity
        {
            ValidFrom = energyPeriodEntity.ValidFrom,
            ValidTo = energyPeriodEntity.ValidTo,
            CreatedAt = DateTimeOffset.UtcNow,
            EnergySupplier = energyPeriodEntity.EnergySupplier,
            WebAccessCode = energyPeriodEntity.WebAccessCode,
            BusinessTransactionDosId = energyPeriodEntity.BusinessTransactionDosId,
        };
    }
}
