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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class MeteringPointPeriodImporter : ITransactionImporter
{
    public Task<TransactionImporterResult> ImportAsync(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(meteringPointTransaction);

        var latestPeriod = meteringPoint.MeteringPointPeriods.MaxBy(x => x.MeteringPointStateId);

        var newPeriod = CreatePeriod(meteringPoint, meteringPointTransaction);

        if (latestPeriod is null)
        {
            meteringPoint.MeteringPointPeriods.Add(newPeriod);
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Handled));
        }

        if (latestPeriod.ValidFrom > meteringPointTransaction.ValidFrom)
        {
            return Task.FromResult(new TransactionImporterResult(TransactionImporterResultStatus.Error, "HTX"));
        }

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

    private static MeteringPointPeriodEntity CreatePeriod(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        return new MeteringPointPeriodEntity
        {
            MeteringPointId = meteringPoint.Id,
            ValidFrom = meteringPointTransaction.ValidFrom,
            ValidTo = meteringPointTransaction.ValidTo,
            CreatedAt = meteringPointTransaction.Dh3Created,
            GridAreaCode = meteringPointTransaction.MeteringGridAreaId,
            OwnedBy = "TBD",
            ConnectionState = ExternalMeteringPointConnectionTypeMapper.Map(meteringPointTransaction.PhysicalStatusOfMp),
            Type = ExternalMeteringPointTypeMapper.Map(meteringPointTransaction.TypeOfMp),
            SubType = ExternalMeteringPointSubTypeMapper.Map(meteringPointTransaction.SubTypeOfMp),
            Resolution = "TBD",
            Unit = ExternalMeteringPointUnitMapper.Map(meteringPointTransaction.EnergyTimeseriesMeasureUnit),
            ProductId = "TBD",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = meteringPointTransaction.MeteringPointStateId,
        };
    }
}
