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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public sealed class ImportHandler : IImportHandler
{
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly IQuarantineZone _quarantineZone;
    private readonly IEnumerable<ITransactionImporter> _transactionImporters;

    public ImportHandler(
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        IQuarantineZone quarantineZone,
        IEnumerable<ITransactionImporter> transactionImporters)
    {
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _quarantineZone = quarantineZone;
        _transactionImporters = transactionImporters;
    }

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        const int limit = 10_000;

        do
        {
            var transaction = await _electricityMarketDatabaseContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (transaction.ConfigureAwait(false))
            {
                var importState = await _electricityMarketDatabaseContext.ImportStates.SingleAsync(cancellationToken).ConfigureAwait(false);

                if (!importState.Enabled)
                {
                    return;
                }

                var offset = importState.Offset;

#pragma warning disable EF1002
                var results = await _electricityMarketDatabaseContext.Database.SqlQueryRaw<MeteringPointGoldenTransactionEntity>(
#pragma warning restore EF1002
                    $"""
                     SELECT TOP {limit} *
                     FROM electricitymarket.GoldenImport
                     WHERE btd_business_trans_doss_id > {importState.Offset}
                     ORDER BY btd_business_trans_doss_id, metering_point_state_id
                     """).ToListAsync(cancellationToken).ConfigureAwait(false);

                foreach (var record in results)
                {
                    var meteringPointTransaction = CreateMeteringPointTransaction(record);

                    offset = meteringPointTransaction.BusinessTransactionDosId;

                    if (await _quarantineZone.IsQuarantinedAsync(meteringPointTransaction).ConfigureAwait(false))
                    {
                        await _quarantineZone.QuarantineAsync(meteringPointTransaction, "Previously quarantined").ConfigureAwait(false);
                        continue;
                    }

                    var meteringPoint = await GetAsync(meteringPointTransaction.Identification).ConfigureAwait(false) ??
                                        await CreateAsync(meteringPointTransaction.Identification).ConfigureAwait(false);

                    await RunImportChainAsync(meteringPoint, meteringPointTransaction).ConfigureAwait(false);
                }

                if (offset == importState.Offset)
                {
                    importState.Enabled = false;
                    await _electricityMarketDatabaseContext.SaveChangesAsync().ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                importState.Offset = offset;

                await _electricityMarketDatabaseContext.SaveChangesAsync().ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    private static MeteringPointTransaction CreateMeteringPointTransaction(MeteringPointGoldenTransactionEntity record)
    {
        return new MeteringPointTransaction(
            record.metering_point_id.ToString(CultureInfo.InvariantCulture),
            record.valid_from_date,
            record.valid_to_date,
            record.dh2_created,
            record.metering_grid_area_id,
            record.metering_point_state_id,
            record.btd_business_trans_doss_id,
            record.physical_status_of_mp,
            record.type_of_mp,
            record.sub_type_of_mp,
            record.energy_timeseries_measure_unit,
            record.web_access_code,
            record.balance_supplier_id);
    }

    private async Task RunImportChainAsync(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        var handled = false;

        foreach (var transactionImporter in _transactionImporters)
        {
            var result = await transactionImporter.ImportAsync(meteringPoint, meteringPointTransaction).ConfigureAwait(false);

            if (result.Status == TransactionImporterResultStatus.Error)
            {
                await _quarantineZone.QuarantineAsync(meteringPointTransaction, result.Message).ConfigureAwait(false);
                return;
            }

            handled |= result.Status == TransactionImporterResultStatus.Handled;
        }

        if (!handled)
        {
            await _quarantineZone.QuarantineAsync(meteringPointTransaction, "Unhandled").ConfigureAwait(false);
        }
    }

    private async Task<MeteringPointEntity?> GetAsync(string identification)
    {
        return await _electricityMarketDatabaseContext.MeteringPoints
            .FirstOrDefaultAsync(x => x.Identification == identification)
            .ConfigureAwait(false);
    }

    private async Task<MeteringPointEntity> CreateAsync(string identification)
    {
        var entity = new MeteringPointEntity
        {
            Identification = identification,
        };

        _electricityMarketDatabaseContext.MeteringPoints.Add(entity);

        await _electricityMarketDatabaseContext.SaveChangesAsync().ConfigureAwait(false);

        return entity;
    }

#pragma warning disable SA1300, CA1707
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private sealed class MeteringPointGoldenTransactionEntity
    {
        public int Id { get; init; }
        public long metering_point_id { get; init; }
        public DateTimeOffset valid_from_date { get; init; }
        public DateTimeOffset valid_to_date { get; init; }
        public DateTimeOffset dh2_created { get; init; }
        public string metering_grid_area_id { get; init; }
        public long metering_point_state_id { get; init; }
        public long btd_business_trans_doss_id { get; init; }
        public string physical_status_of_mp { get; init; }
        public string type_of_mp { get; init; }
        public string sub_type_of_mp { get; init; }
        public string energy_timeseries_measure_unit { get; init; }
        public string web_access_code { get; init; }
        public string balance_supplier_id { get; init; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore SA1300, CA1707
}
