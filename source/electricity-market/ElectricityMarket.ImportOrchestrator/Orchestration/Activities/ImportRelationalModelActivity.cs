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

using System.Diagnostics;
using System.Globalization;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class ImportRelationalModelActivity
{
    private readonly ILogger<ImportRelationalModelActivity> _logger;
    private readonly IDbContextFactory<ElectricityMarketDatabaseContext> _contextFactory;
    private readonly IEnumerable<ITransactionImporter> _transactionImporters;

    public ImportRelationalModelActivity(
        ILogger<ImportRelationalModelActivity> logger,
        IDbContextFactory<ElectricityMarketDatabaseContext> contextFactory,
        IEnumerable<ITransactionImporter> transactionImporters)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _transactionImporters = transactionImporters;
    }

    [Function(nameof(ImportRelationalModelActivity))]
    public async Task RunAsync([ActivityTrigger] ImportRelationalModelActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        _logger.LogWarning("Import started {skip} - {take}", input.Skip, input.Take);

        var sw = Stopwatch.StartNew();

        var writeContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        writeContext.ChangeTracker.AutoDetectChangesEnabled = false;
        writeContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        var transaction = await writeContext.Database.BeginTransactionAsync().ConfigureAwait(false);

        await using (writeContext)
        {
            await using (transaction.ConfigureAwait(false))
            {
                var readContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

                await using (readContext)
                {
                    var meteringPointsQuery = readContext.ImportedTransactions
                        .AsNoTracking()
                        .Select(t => t.metering_point_id)
                        .Distinct()
                        .OrderBy(x => x)
                        .Skip(input.Skip)
                        .Take(input.Take);

                    var query = readContext.ImportedTransactions
                        .AsNoTracking()
                        .Join(
                            meteringPointsQuery,
                            outer => outer.metering_point_id,
                            inner => inner,
                            (entity, _) => entity)
                        .OrderBy(t => t.metering_point_id)
                        .ThenBy(t => t.btd_business_trans_doss_id)
                        .ThenBy(t => t.metering_point_state_id)
                        .AsAsyncEnumerable();


                    var currentMeteringPointId = string.Empty;
                    var list = new List<MeteringPointTransaction>();

                    await foreach (var record in query)
                    {
                        var meteringPointTransaction = CreateMeteringPointTransaction(record);

                        if (list.Count != 0 && currentMeteringPointId != meteringPointTransaction.Identification)
                        {
                            var mp = new MeteringPointEntity
                            {
                                Identification = currentMeteringPointId,
                            };

                            foreach (var mpt in list)
                            {
                                await RunImportChainAsync(mp, mpt).ConfigureAwait(false);
                            }

                            writeContext.MeteringPoints.Add(mp);

                            list.Clear();
                            currentMeteringPointId = meteringPointTransaction.Identification;
                        }

                        list.Add(meteringPointTransaction);
                    }

                    await writeContext.SaveChangesAsync().ConfigureAwait(false);

                    await transaction.CommitAsync().ConfigureAwait(false);
                }

                _logger.LogWarning("Imported batch {skip} - {take} in {ElapsedMilliseconds} ms", input.Skip, input.Take, sw.ElapsedMilliseconds);
            }
        }
    }

    private static MeteringPointTransaction CreateMeteringPointTransaction(ImportedTransactionEntity record)
    {
        return new MeteringPointTransaction(
            record.metering_point_id.ToString(CultureInfo.InvariantCulture),
            record.valid_from_date,
            record.valid_to_date,
            record.dh2_created,
            record.metering_grid_area_id.TrimEnd(),
            record.metering_point_state_id,
            record.btd_business_trans_doss_id,
            record.physical_status_of_mp.TrimEnd(),
            record.type_of_mp.TrimEnd(),
            record.sub_type_of_mp.TrimEnd(),
            record.energy_timeseries_measure_unit.TrimEnd(),
            record.web_access_code?.TrimEnd(),
            record.balance_supplier_id?.TrimEnd());
    }

    private async Task RunImportChainAsync(MeteringPointEntity meteringPoint, MeteringPointTransaction meteringPointTransaction)
    {
        var handled = false;

        foreach (var transactionImporter in _transactionImporters)
        {
            var result = await transactionImporter.ImportAsync(meteringPoint, meteringPointTransaction).ConfigureAwait(false);

            handled |= result.Status == TransactionImporterResultStatus.Handled;
        }

        if (!handled)
        {
            _logger.LogWarning("Unhandled transaction trans_dos_id: {TransactionId} state_id: {StateId}", meteringPointTransaction.BusinessTransactionDosId, meteringPointTransaction.MeteringPointStateId);
        }
    }
}
