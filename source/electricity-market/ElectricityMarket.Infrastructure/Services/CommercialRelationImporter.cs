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
using NodaTime;

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
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        var latestRelation = meteringPoint.CommercialRelations
            .Where(x => x.StartDate < x.EndDate)
            .MaxBy(x => x.EndDate);

        var latestEnergyPeriod = latestRelation?
            .EnergyPeriods
            .Single(x => x.ValidTo == Instant.MaxValue && x.RetiredById is null);

        var newRelation = CreateRelation(meteringPoint, meteringPointTransaction);

        // This is the first time we encounter this metering point, so we have no current relations
        if (latestRelation is null)
        {
            meteringPoint.CommercialRelations.Add(newRelation);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        if (!string.Equals(meteringPointTransaction.WebAccessCode, latestEnergyPeriod?.WebAccessCode, StringComparison.OrdinalIgnoreCase))
        {
            // MoveIn
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }
        else if (!string.Equals(meteringPointTransaction.EnergySupplier, latestRelation.EnergySupplier, StringComparison.OrdinalIgnoreCase))
        {
            // ChangeSupplier
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Unhandled));
    }

    private static CommercialRelationEntity CreateRelation(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        return new CommercialRelationEntity
        {
            MeteringPointId = meteringPoint.Id,
            StartDate = meteringPointTransaction.ValidFrom,
            EndDate = meteringPointTransaction.ValidTo,
            EnergySupplier = meteringPointTransaction.EnergySupplier,
            ModifiedAt = SystemClock.Instance.GetCurrentInstant(),
            EnergyPeriods = new List<EnergyPeriodEntity>
            {
                CreateEnergyPeriod(meteringPointTransaction)
            }
        };
    }

    private static EnergyPeriodEntity CreateEnergyPeriod(MeteringPointTransaction meteringPointTransaction)
    {
        return new EnergyPeriodEntity
        {
            ValidFrom = meteringPointTransaction.ValidFrom,
            ValidTo = meteringPointTransaction.ValidTo,
            EnergySupplier = meteringPointTransaction.EnergySupplier,
            WebAccessCode = meteringPointTransaction.WebAccessCode,
            BusinessTransactionDosId = meteringPointTransaction.BusinessTransactionDosId,
        };
    }
}
