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
                            ["Id", "Identification", "Version"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.MeteringPointPeriods).Select(mpp => mpp.InstallationAddress),
                            "InstallationAddress",
                            ["Id", "StreetCode", "StreetName", "BuildingNumber", "CityName", "CitySubdivisionName", "DarReference", "WashInstructions", "CountryCode", "Floor", "Room", "PostCode", "MunicipalityCode", "LocationDescription"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.MeteringPointPeriods),
                            "MeteringPointPeriod",
                            ["Id", "MeteringPointId", "ValidFrom", "ValidTo", "RetiredById", "RetiredAt", "CreatedAt", "ParentIdentification", "Type", "SubType", "ConnectionState", "Resolution", "GridAreaCode", "OwnedBy", "ConnectionType", "DisconnectionType", "Product", "ProductObligation", "MeasureUnit", "AssetType", "FuelType", "Capacity", "PowerLimitKw", "PowerLimitA", "MeterNumber", "SettlementGroup", "ScheduledMeterReadingMonth", "ExchangeFromGridArea", "ExchangeToGridArea", "PowerPlantGsrn", "SettlementMethod", "InstallationAddressId", "MeteringPointStateId", "BusinessTransactionDosId", "TransactionType"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.CommercialRelations),
                            "CommercialRelation",
                            ["Id", "MeteringPointId", "EnergySupplier", "StartDate", "EndDate", "ModifiedAt", "ClientId"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.CommercialRelations.SelectMany(cr => cr.ElectricalHeatingPeriods)),
                            "ElectricalHeatingPeriod",
                            ["Id", "CommercialRelationId", "ValidFrom", "ValidTo", "CreatedAt", "RetiredById", "RetiredAt"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.CommercialRelations.SelectMany(cr => cr.EnergySupplyPeriods)),
                            "EnergySupplyPeriod",
                            ["Id", "CommercialRelationId", "ValidFrom", "ValidTo", "RetiredById", "RetiredAt", "CreatedAt", "WebAccessCode", "EnergySupplier", "BusinessTransactionDosId"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.CommercialRelations.SelectMany(cr => cr.EnergySupplyPeriods.SelectMany(esp => esp.Contacts))).Select(c => c.ContactAddress).Where(ca => ca != null),
                            "ContactAddress",
                            ["Id", "IsProtectedAddress", "Attention", "StreetCode", "StreetName", "BuildingNumber", "CityName", "CitySubdivisionName", "DarReference", "CountryCode", "Floor", "Room", "PostCode", "MunicipalityCode"])
                        .ConfigureAwait(false);

                    await BulkInsertAsync(
                            sqlConnection,
                            transaction,
                            batch.SelectMany(b => b.CommercialRelations.SelectMany(cr => cr.EnergySupplyPeriods.SelectMany(esp => esp.Contacts))),
                            "Contact",
                            ["Id", "EnergySupplyPeriodId", "RelationType", "DisponentName", "Cpr", "Cvr", "ContactAddressId", "ContactName", "Email", "Phone", "Mobile", "IsProtectedName"])
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
