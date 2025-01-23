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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class MeteringPointPeriodImporter : ITransactionImporter
{
    private readonly IElectricityMarketDatabaseContext _context;

    public MeteringPointPeriodImporter(IElectricityMarketDatabaseContext context)
    {
        _context = context;
    }

    public async Task<TransactionImporterResult> ImportAsync(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(meteringPointTransaction);

        var query =
            from meteringPointPeriod in _context.MeteringPointPeriods
            where meteringPointPeriod.MeteringPointId == meteringPoint.Id &&
                  meteringPointPeriod.ValidTo == Instant.MaxValue
            orderby meteringPointPeriod.ValidFrom descending
            select meteringPointPeriod;

        var latestMeteringPointPeriod = await query.FirstOrDefaultAsync().ConfigureAwait(false);

        if (latestMeteringPointPeriod is null)
        {
            _context.MeteringPointPeriods.Add(CreatePeriod(meteringPoint, meteringPointTransaction));
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return new TransactionImporterResult(TransactionImporterResultStatus.Handled);
        }

        if (latestMeteringPointPeriod.ValidFrom >= meteringPointTransaction.ValidFrom)
        {
            return new TransactionImporterResult(TransactionImporterResultStatus.Error, "HTX");
        }

        await RetireAsync(latestMeteringPointPeriod, meteringPointTransaction).ConfigureAwait(false);

        _context.MeteringPointPeriods.Add(CreatePeriod(meteringPoint, meteringPointTransaction));

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return new TransactionImporterResult(TransactionImporterResultStatus.Handled);
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
            ConnectionState = ExternalMeteringPointConnectionTypeMapper.Map(meteringPointTransaction.MpConnectionType),
            Type = ExternalMeteringPointTypeMapper.Map(meteringPointTransaction.TypeOfMp),
            SubType = ExternalMeteringPointSubTypeMapper.Map(meteringPointTransaction.SubTypeOfMp),
            Resolution = "TBD",
            Unit = ExternalMeteringPointUnitMapper.Map(meteringPointTransaction.EnergyTimeseriesMeasureUnit),
            ProductId = "TBD",
            ScheduledMeterReadingMonth = 1,
            MeteringPointStateId = meteringPointTransaction.MeteringPointStateId,
        };
    }

    private async Task RetireAsync(MeteringPointPeriodEntity latestMeteringPointPeriod, MeteringPointTransaction meteringPointTransaction)
    {
        var copy = new MeteringPointPeriodEntity
        {
            MeteringPointId = latestMeteringPointPeriod.MeteringPointId,
            ValidFrom = latestMeteringPointPeriod.ValidFrom,
            ValidTo = meteringPointTransaction.ValidFrom,
            CreatedAt = latestMeteringPointPeriod.CreatedAt,
            GridAreaCode = latestMeteringPointPeriod.GridAreaCode,
            OwnedBy = latestMeteringPointPeriod.OwnedBy,
            ConnectionState = latestMeteringPointPeriod.ConnectionState,
            Type = latestMeteringPointPeriod.Type,
            SubType = latestMeteringPointPeriod.SubType,
            Resolution = latestMeteringPointPeriod.Resolution,
            Unit = latestMeteringPointPeriod.Unit,
            ProductId = latestMeteringPointPeriod.ProductId,
            ScheduledMeterReadingMonth = latestMeteringPointPeriod.ScheduledMeterReadingMonth,
            MeteringPointStateId = meteringPointTransaction.MeteringPointStateId,
            BusinessTransactionDosId = meteringPointTransaction.BusinessTransactionDosId,
        };

        _context.MeteringPointPeriods.Add(copy);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        latestMeteringPointPeriod.RetiredById = copy.Id;
        latestMeteringPointPeriod.RetiredAt = SystemClock.Instance.GetCurrentInstant();
    }
}
