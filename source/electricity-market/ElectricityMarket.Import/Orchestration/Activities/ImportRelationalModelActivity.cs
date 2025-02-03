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

using System.Globalization;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElectricityMarket.Import.Orchestration.Activities;

public sealed class ImportRelationalModelActivity
{
    private readonly ILogger<ImportRelationalModelActivity> _logger;
    private readonly IDbContextFactory<ElectricityMarketDatabaseContext> _contextFactory;
    private readonly IElectricityMarketDatabaseContext _electricityMarketDatabaseContext;
    private readonly IQuarantineZone _quarantineZone;
    private readonly IEnumerable<ITransactionImporter> _transactionImporters;

    public ImportRelationalModelActivity(
        ILogger<ImportRelationalModelActivity> logger,
        IDbContextFactory<ElectricityMarketDatabaseContext> contextFactory,
        IElectricityMarketDatabaseContext electricityMarketDatabaseContext,
        IQuarantineZone quarantineZone,
        IEnumerable<ITransactionImporter> transactionImporters)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _electricityMarketDatabaseContext = electricityMarketDatabaseContext;
        _quarantineZone = quarantineZone;
        _transactionImporters = transactionImporters;
    }

    [Function(nameof(ImportRelationalModelActivity))]
    public async Task RunAsync([ActivityTrigger] ActivityInput input)
    {
        _logger.LogWarning("Hash: {Hash}", _electricityMarketDatabaseContext.GetHashCode());

        ArgumentNullException.ThrowIfNull(input);
        var transaction = await _electricityMarketDatabaseContext.Database.BeginTransactionAsync().ConfigureAwait(false);

        await using (transaction.ConfigureAwait(false))
        {
            var (skip, take) = input.MeteringPointRange;

            var readContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

            await using (readContext)
            {
#pragma warning disable EF1002
                var result = readContext.ImportedTransactions.FromSqlRaw(
#pragma warning restore EF1002
                    $"""
                     WITH SelectedGroups AS (
                         SELECT DISTINCT metering_point_id
                         FROM electricitymarket.GoldenImport
                         ORDER BY metering_point_id
                         OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY
                     )
                     SELECT i.*
                     FROM electricitymarket.GoldenImport i
                     JOIN SelectedGroups sg ON i.metering_point_id = sg.metering_point_id
                     ORDER BY i.metering_point_id, i.btd_business_trans_doss_id, i.metering_point_state_id;
                     """).AsAsyncEnumerable();

                var currentMeteringPointId = string.Empty;
                var list = new List<MeteringPointTransaction>();

                await foreach (var record in result)
                {
                    var meteringPointTransaction = CreateMeteringPointTransaction(record);

                    if (list.Count != 0 && currentMeteringPointId != meteringPointTransaction.Identification)
                    {
                        // persist batch
                        var mp = new MeteringPointEntity
                        {
                            Identification = currentMeteringPointId,
                        };

                        foreach (var mpt in list)
                        {
                            await RunImportChainAsync(mp, mpt).ConfigureAwait(false);
                        }

                        _electricityMarketDatabaseContext.MeteringPoints.Add(mp);

                        list.Clear();
                        currentMeteringPointId = meteringPointTransaction.Identification;
                    }

                    list.Add(meteringPointTransaction);

                    // currentMeteringPointId = meteringPointTransaction.Identification;
                    //
                    // if (await _quarantineZone.IsQuarantinedAsync(meteringPointTransaction).ConfigureAwait(false))
                    // {
                    //     await _quarantineZone.QuarantineAsync(meteringPointTransaction, "Previously quarantined").ConfigureAwait(false);
                    //     continue;
                    // }
                    //
                    // var meteringPoint = await GetAsync(meteringPointTransaction.Identification).ConfigureAwait(false) ??
                    //                     await CreateAsync(meteringPointTransaction.Identification).ConfigureAwait(false);
                    //
                    // await RunImportChainAsync(meteringPoint, meteringPointTransaction).ConfigureAwait(false);
                }

                await _electricityMarketDatabaseContext.SaveChangesAsync().ConfigureAwait(false);

                await transaction.CommitAsync().ConfigureAwait(false);
            }

            _logger.LogWarning($"Done {input.MeteringPointRange.Skip} {input.MeteringPointRange.Take}");
        }
    }

    private static MeteringPointTransaction CreateMeteringPointTransaction(ImportedTransactionEntity record)
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

            // if (result.Status == TransactionImporterResultStatus.Error)
            // {
            //     await _quarantineZone.QuarantineAsync(meteringPointTransaction, result.Message).ConfigureAwait(false);
            //     return;
            // }
            handled |= result.Status == TransactionImporterResultStatus.Handled;
        }

        // if (!handled)
        // {
        //     await _quarantineZone.QuarantineAsync(meteringPointTransaction, "Unhandled").ConfigureAwait(false);
        // }
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

#pragma warning disable CA1034
    public class ActivityInput
    {
        public Page MeteringPointRange { get; set; } = null!;

        public record Page
        {
            public int Skip { get; set; }
            public int Take { get; set; }

            public void Deconstruct(out object skip, out object take)
            {
                skip = Skip;
                take = Take;
            }
        }
    }
#pragma warning restore CA1034
}
