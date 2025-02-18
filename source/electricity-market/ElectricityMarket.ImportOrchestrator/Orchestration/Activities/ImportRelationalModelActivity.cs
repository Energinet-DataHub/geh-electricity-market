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

using System.Collections.Concurrent;
using System.Diagnostics;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using FastMember;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class ImportRelationalModelActivity : IDisposable
{
    private readonly BlockingCollection<List<ImportedTransactionEntity>> _importedTransactions = new(500000);
    private readonly BlockingCollection<List<MeteringPointEntity>> _relationalModelBatches = new(10);
    private readonly List<List<QuarantinedMeteringPointEntity>> _quarantined = new(10);

    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly IDbContextFactory<ElectricityMarketDatabaseContext> _contextFactory;
    private readonly IMeteringPointImporter _meteringPointImporter;
    private readonly ILogger<ImportRelationalModelActivity> _logger;

    public ImportRelationalModelActivity(
        IOptions<DatabaseOptions> databaseOptions,
        IDbContextFactory<ElectricityMarketDatabaseContext> contextFactory,
        IMeteringPointImporter meteringPointImporter,
        ILogger<ImportRelationalModelActivity> logger)
    {
        _databaseOptions = databaseOptions;
        _contextFactory = contextFactory;
        _meteringPointImporter = meteringPointImporter;
        _logger = logger;
    }

    [Function(nameof(ImportRelationalModelActivity))]
    public async Task RunAsync([ActivityTrigger] ImportRelationalModelActivityInput input)
    {
        var read = Task.Run(async () =>
        {
            try
            {
                await ReadImportedTransactionsAsync(input.Skip, input.Take).ConfigureAwait(false);
            }
            catch
            {
                _importedTransactions.Dispose();
                throw;
            }
        });

        var package = Task.Run(async () =>
        {
            try
            {
                await ImportAndPackageTransactionsAsync(input.Skip + 1).ConfigureAwait(false);
            }
            catch
            {
                _relationalModelBatches.Dispose();
                throw;
            }
        });

        var write = Task.Run(WriteRelationalModelAsync);

        await Task.WhenAll(read, package, write).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _importedTransactions.Dispose();
        _relationalModelBatches.Dispose();
    }

    private static void AssignPrimaryKeys(MeteringPointEntity meteringPointEntity, long initialMeteringPointPrimaryKey)
    {
        meteringPointEntity.Id = initialMeteringPointPrimaryKey;

        // MeteringPointPeriod
        {
            var meteringPointPeriodPrimaryKey = meteringPointEntity.Id * 10000;

            foreach (var meteringPointPeriodEntity in meteringPointEntity.MeteringPointPeriods)
            {
                meteringPointPeriodEntity.Id = meteringPointPeriodPrimaryKey++;
                meteringPointPeriodEntity.MeteringPointId = meteringPointEntity.Id;
            }

            foreach (var meteringPointPeriodEntity in meteringPointEntity.MeteringPointPeriods)
            {
                meteringPointPeriodEntity.RetiredById = meteringPointPeriodEntity.RetiredBy?.Id;
            }

            if (meteringPointPeriodPrimaryKey >= (meteringPointEntity.Id * 10000) + 10000)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, MeteringPointPeriod.");
        }

        // CommercialRelation
        {
            var commercialRelationPrimaryKey = meteringPointEntity.Id * 10000;

            foreach (var commercialRelationEntity in meteringPointEntity.CommercialRelations)
            {
                commercialRelationEntity.Id = commercialRelationPrimaryKey++;
                commercialRelationEntity.MeteringPointId = meteringPointEntity.Id;
            }

            if (commercialRelationPrimaryKey >= (meteringPointEntity.Id * 10000) + 10000)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, CommercialRelation.");
        }

        // EnergySupplyPeriod
        foreach (var commercialRelationEntity in meteringPointEntity.CommercialRelations)
        {
            var energySupplyPeriodPrimaryKey = commercialRelationEntity.Id * 10000;

            foreach (var energySupplyPeriodEntity in commercialRelationEntity.EnergySupplyPeriods)
            {
                energySupplyPeriodEntity.Id = energySupplyPeriodPrimaryKey++;
                energySupplyPeriodEntity.CommercialRelationId = commercialRelationEntity.Id;
            }

            foreach (var energySupplyPeriodEntity in commercialRelationEntity.EnergySupplyPeriods)
            {
                energySupplyPeriodEntity.RetiredById = energySupplyPeriodEntity.RetiredBy?.Id;
            }

            if (energySupplyPeriodPrimaryKey >= (commercialRelationEntity.Id * 10000) + 10000)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, EnergySupplyPeriod.");
        }
    }

    private async Task ReadImportedTransactionsAsync(int skip, int take)
    {
        var readContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        readContext.Database.SetCommandTimeout(60 * 5);

        await using (readContext.ConfigureAwait(false))
        {
            var meteringPointsQuery = readContext.ImportedTransactions
                .AsNoTracking()
                .Select(t => t.metering_point_id)
                .Distinct()
                .OrderBy(meteringPointId => meteringPointId)
                .Skip(skip)
                .Take(take);

            var importedTransactionsStream = readContext.ImportedTransactions
                .AsNoTracking()
                .Join(
                    meteringPointsQuery,
                    outer => outer.metering_point_id,
                    inner => inner,
                    (entity, _) => entity)
                .OrderBy(t => t.metering_point_id)
                .ThenBy(t => t.btd_trans_doss_id)
                .ThenBy(t => t.metering_point_state_id)
                .AsAsyncEnumerable();

            var transactionsForOneMp = new List<ImportedTransactionEntity>();

            await foreach (var importedTransaction in importedTransactionsStream.ConfigureAwait(false))
            {
                if (transactionsForOneMp.Count > 0 &&
                    transactionsForOneMp[0].metering_point_id != importedTransaction.metering_point_id)
                {
                    _importedTransactions.Add(transactionsForOneMp);
                    transactionsForOneMp = new List<ImportedTransactionEntity>();
                }

                transactionsForOneMp.Add(importedTransaction);
            }

            if (transactionsForOneMp.Count != 0)
            {
                _importedTransactions.Add(transactionsForOneMp);
            }

            _importedTransactions.CompleteAdding();
            _logger.LogWarning("All imported transactions for range {BeginRange} were read.", skip);
        }
    }

    private async Task ImportAndPackageTransactionsAsync(long initialMeteringPointPrimaryKey)
    {
        const int capacity = 30000;
        const int quarantineCapacity = 30000;

        var sw = Stopwatch.StartNew();
        var batch = new List<MeteringPointEntity>(capacity);
        var quarantineBatch = new List<QuarantinedMeteringPointEntity>(quarantineCapacity);

        foreach (var transactionsForOneMp in _importedTransactions.GetConsumingEnumerable())
        {
            if (batch.Count == capacity)
            {
                _relationalModelBatches.Add(batch);
                _logger.LogWarning("A relational batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

                sw = Stopwatch.StartNew();
                batch = new List<MeteringPointEntity>(capacity);
            }

            if (quarantineBatch.Count == quarantineCapacity)
            {
                _quarantined.Add(quarantineBatch);
                quarantineBatch = new List<QuarantinedMeteringPointEntity>(capacity);
            }

            var meteringPoint = new MeteringPointEntity();

            var (imported, message) = await _meteringPointImporter
                .ImportAsync(meteringPoint, transactionsForOneMp)
                .ConfigureAwait(false);

            if (imported)
            {
                AssignPrimaryKeys(meteringPoint, initialMeteringPointPrimaryKey++);
                batch.Add(meteringPoint);
            }
            else
            {
                // add to quarantine
                quarantineBatch.Add(new QuarantinedMeteringPointEntity
                {
                    Identification = meteringPoint.Identification,
                    Message = message,
                });
            }
        }

        if (batch.Count > 0)
        {
            _relationalModelBatches.Add(batch);
        }

        if (quarantineBatch.Count > 0)
        {
            _quarantined.Add(quarantineBatch);
        }

        _relationalModelBatches.CompleteAdding();
    }

    private async Task WriteRelationalModelAsync()
    {
        var sqlConnection = new SqlConnection(_databaseOptions.Value.ConnectionString);
        await using (sqlConnection.ConfigureAwait(false))
        {
            await sqlConnection.OpenAsync().ConfigureAwait(false);

#pragma warning disable CA1849 // Cannot use Async version due to type constrains from SqlBulkCopy.
            var transaction = sqlConnection.BeginTransaction();
#pragma warning restore CA1849

            await using (transaction.ConfigureAwait(false))
            {
                foreach (var batch in _relationalModelBatches.GetConsumingEnumerable())
                {
                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch,
                            "MeteringPoint",
                            ["Id", "Identification"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.MeteringPointPeriods),
                            "MeteringPointPeriod",
                            ["Id", "MeteringPointId", "ValidFrom", "ValidTo", "RetiredById", "RetiredAt", "CreatedAt", "GridAreaCode", "OwnedBy", "ConnectionState", "Type", "SubType", "Resolution", "Unit", "ProductId", "SettlementGroup", "ScheduledMeterReadingMonth", "ParentIdentification", "MeteringPointStateId", "BusinessTransactionDosId", "EffectuationDate", "TransactionType"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.CommercialRelations),
                            "CommercialRelation",
                            ["Id", "MeteringPointId", "EnergySupplier", "StartDate", "EndDate", "ModifiedAt", "CustomerId"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.CommercialRelations.SelectMany(cr => cr.EnergySupplyPeriods)),
                            "EnergySupplyPeriod",
                            ["Id", "CommercialRelationId", "ValidFrom", "ValidTo", "RetiredById", "RetiredAt", "CreatedAt", "WebAccessCode", "EnergySupplier", "BusinessTransactionDosId"])
                        .ConfigureAwait(false);
                }

                foreach (var quarantineBatch in _quarantined)
                {
                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            quarantineBatch,
                            "QuarantinedMeteringPoint",
                            ["Id", "Identification", "Message"])
                        .ConfigureAwait(false);
                }

                await transaction.CommitAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task BulkInsertAsync<T>(
        SqlConnection connection,
        SqlTransaction transaction,
        IEnumerable<T> dataSource,
        string tableName,
        string[] columns)
    {
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);

        bulkCopy.DestinationTableName = $"electricitymarket.{tableName}";
        bulkCopy.BulkCopyTimeout = 0;

        var sw = Stopwatch.StartNew();

        var dataReader = ObjectReader.Create(dataSource, columns);
        await using (dataReader.ConfigureAwait(false))
        {
            await bulkCopy.WriteToServerAsync(dataReader).ConfigureAwait(false);
        }

        _logger.LogWarning("A batch was inserted into {TableName} in {InsertTime} ms.", tableName, sw.ElapsedMilliseconds);
    }
}
