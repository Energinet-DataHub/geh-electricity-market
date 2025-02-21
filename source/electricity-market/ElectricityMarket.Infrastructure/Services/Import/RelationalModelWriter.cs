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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using FastMember;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class RelationalModelWriter : IRelationalModelWriter
{
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly ILogger<RelationalModelWriter> _logger;

    public RelationalModelWriter(IOptions<DatabaseOptions> databaseOptions, ILogger<RelationalModelWriter> logger)
    {
        _databaseOptions = databaseOptions;
        _logger = logger;
    }

    public async Task WriteRelationalModelAsync(
        IEnumerable<IList<MeteringPointEntity>> relationalModelBatches,
        IEnumerable<IList<QuarantinedMeteringPointEntity>> quarantined)
    {
        ArgumentNullException.ThrowIfNull(relationalModelBatches);
        ArgumentNullException.ThrowIfNull(quarantined);

        var sqlConnection = new SqlConnection(_databaseOptions.Value.ConnectionString);
        await using (sqlConnection.ConfigureAwait(false))
        {
            await sqlConnection.OpenAsync().ConfigureAwait(false);

#pragma warning disable CA1849 // Cannot use Async version due to type constrains from SqlBulkCopy.
            var transaction = sqlConnection.BeginTransaction();
#pragma warning restore CA1849

            await using (transaction.ConfigureAwait(false))
            {
                foreach (var batch in relationalModelBatches)
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

                foreach (var quarantineBatch in quarantined)
                {
                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            quarantineBatch,
                            "QuarantinedMeteringPoint",
                            ["Id", "Identification", "Message"],
                            false)
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
        string[] columns,
        bool useIdentity = true)
    {
        using var bulkCopy = new SqlBulkCopy(connection, useIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default, transaction);

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
