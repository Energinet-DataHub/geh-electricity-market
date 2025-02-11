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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class MeteringPointPeriodImporter : ITransactionImporter
{
    public Task<TransactionImporterResult> ImportAsync(MeteringPointEntity meteringPoint, ImportedTransactionEntity importedTransactionEntity)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(importedTransactionEntity);

        var latestPeriod = meteringPoint.MeteringPointPeriods.MaxBy(x => x.MeteringPointStateId);

        var newPeriod = CreatePeriod(meteringPoint, importedTransactionEntity);

        if (latestPeriod is null)
        {
            meteringPoint.MeteringPointPeriods.Add(newPeriod);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        if (latestPeriod.ValidFrom > importedTransactionEntity.valid_from_date)
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Error, "HTX"));

        if (IsLatestPeriodRetired(latestPeriod, newPeriod))
        {
            latestPeriod.RetiredBy = newPeriod;
            meteringPoint.MeteringPointPeriods.Add(newPeriod);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        if (latestPeriod.ValidTo != DateTimeOffset.MaxValue && newPeriod.ValidFrom > latestPeriod.ValidFrom)
        {
            meteringPoint.MeteringPointPeriods.Add(newPeriod);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Unhandled));
    }

    private static bool IsLatestPeriodRetired(MeteringPointPeriodEntity latestMeteringPointPeriod, MeteringPointPeriodEntity incomingMeteringPointPeriod)
    {
        return latestMeteringPointPeriod.ValidTo == DateTimeOffset.MaxValue && incomingMeteringPointPeriod.ValidFrom == latestMeteringPointPeriod.ValidFrom;
    }

    private static MeteringPointPeriodEntity CreatePeriod(MeteringPointEntity meteringPoint, ImportedTransactionEntity importedTransactionEntity)
    {
        return new MeteringPointPeriodEntity
        {
            MeteringPointId = meteringPoint.Id,
            ValidFrom = importedTransactionEntity.valid_from_date,
            ValidTo = importedTransactionEntity.valid_to_date,
            CreatedAt = importedTransactionEntity.dh2_created,
            GridAreaCode = importedTransactionEntity.metering_grid_area_id,
            OwnedBy = "TBD",
            ConnectionState = ExternalMeteringPointConnectionStateMapper.Map(importedTransactionEntity.physical_status_of_mp.TrimEnd()),
            Type = ExternalMeteringPointTypeMapper.Map(importedTransactionEntity.type_of_mp.TrimEnd()),
            SubType = ExternalMeteringPointSubTypeMapper.Map(importedTransactionEntity.sub_type_of_mp.TrimEnd()),
            Resolution = "TBD",
            Unit = ExternalMeteringPointUnitMapper.Map(importedTransactionEntity.energy_timeseries_measure_unit.TrimEnd()),
            ProductId = "TBD",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = importedTransactionEntity.metering_point_state_id,
        };
    }
}
