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
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using FastMember;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectricityMarket.ImportOrchestrator.Orchestration.Activities;

public sealed class ImportGoldModelActivity : IDisposable
{
    private static readonly IEnumerable<KeyValuePair<string, Action<object?, ImportedTransactionEntity>>> _importFields =
    [
        MapProperty(t => t.metering_point_id),
        MapProperty(t => t.valid_from_date),
        MapProperty(t => t.valid_to_date, DateTimeOffset.MaxValue),
        MapProperty(t => t.dh2_created),
        MapProperty(t => t.metering_grid_area_id),
        MapProperty(t => t.metering_point_state_id),
        MapProperty(t => t.btd_trans_doss_id, -1),
        MapProperty(t => t.parent_metering_point_id),
        MapProperty(t => t.type_of_mp),
        MapProperty(t => t.sub_type_of_mp),
        MapProperty(t => t.physical_status_of_mp),
        MapProperty(t => t.web_access_code),
        MapProperty(t => t.balance_supplier_id),
        MapProperty(t => t.effectuation_date, DateTimeOffset.MaxValue),
        MapProperty(t => t.transaction_type, string.Empty),
        MapProperty(t => t.meter_reading_occurrence),
        MapProperty(t => t.mp_connection_type),
        MapProperty(t => t.disconnection_type),
        MapProperty(t => t.product),
        MapProperty(t => t.product_obligation),
        MapProperty(t => t.energy_timeseries_measure_unit),
        MapProperty(t => t.asset_type),
        MapProperty(t => t.fuel_type),
        MapProperty(t => t.mp_capacity),
        MapProperty(t => t.power_limit_kw),
        MapProperty(t => t.power_limit_a),
        MapProperty(t => t.meter_number),
        MapProperty(t => t.net_settlement_group),
        MapProperty(t => t.scheduled_meter_reading_date01),
        MapProperty(t => t.from_grid_area),
        MapProperty(t => t.to_grid_area),
        MapProperty(t => t.power_plant_gsrn),
        MapProperty(t => t.settlement_method),
        MapProperty(t => t.location_street_code),
        MapProperty(t => t.location_street_name),
        MapProperty(t => t.location_building_number),
        MapProperty(t => t.location_city_name),
        MapProperty(t => t.location_city_subdivision_name),
        MapProperty(t => t.location_dar_reference),
        MapProperty(t => t.location_country_name),
        MapProperty(t => t.location_floor_id),
        MapProperty(t => t.location_room_id),
        MapProperty(t => t.location_postcode),
        MapProperty(t => t.location_municipality_code),
        MapProperty(t => t.location_location_description),
        MapProperty(t => t.first_consumer_party_name),
        MapProperty(t => t.second_consumer_party_name),
        MapProperty(t => t.consumer_cvr),
        MapProperty(t => t.protected_name),
        MapProperty(t => t.contact_1_contact_name1),
        MapProperty(t => t.contact_1_protected_address),
        MapProperty(t => t.contact_1_phone_number),
        MapProperty(t => t.contact_1_mobile_number),
        MapProperty(t => t.contact_1_email_address),
        MapProperty(t => t.contact_1_attention),
        MapProperty(t => t.contact_1_street_code),
        MapProperty(t => t.contact_1_street_name),
        MapProperty(t => t.contact_1_building_number),
        MapProperty(t => t.contact_1_postcode),
        MapProperty(t => t.contact_1_city_name),
        MapProperty(t => t.contact_1_city_subdivision_name),
        MapProperty(t => t.contact_1_dar_reference),
        MapProperty(t => t.contact_1_country_name),
        MapProperty(t => t.contact_1_floor_id),
        MapProperty(t => t.contact_1_room_id),
        MapProperty(t => t.contact_1_post_box),
        MapProperty(t => t.contact_1_municipality_code),
        MapProperty(t => t.contact_4_contact_name1),
        MapProperty(t => t.contact_4_protected_address),
        MapProperty(t => t.contact_4_phone_number),
        MapProperty(t => t.contact_4_mobile_number),
        MapProperty(t => t.contact_4_email_address),
        MapProperty(t => t.contact_4_attention),
        MapProperty(t => t.contact_4_street_code),
        MapProperty(t => t.contact_4_street_name),
        MapProperty(t => t.contact_4_building_number),
        MapProperty(t => t.contact_4_postcode),
        MapProperty(t => t.contact_4_city_name),
        MapProperty(t => t.contact_4_city_subdivision_name),
        MapProperty(t => t.contact_4_dar_reference),
        MapProperty(t => t.contact_4_country_name),
        MapProperty(t => t.contact_4_floor_id),
        MapProperty(t => t.contact_4_room_id),
        MapProperty(t => t.contact_4_post_box),
        MapProperty(t => t.contact_4_municipality_code),
    ];

    private readonly BlockingCollection<ExpandoObject> _importCollection = new(1000000);
    private readonly BlockingCollection<IDataReader> _submitCollection = new(5);

    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly ILogger<ImportGoldModelActivity> _logger;

    public ImportGoldModelActivity(
        IOptions<DatabaseOptions> databaseOptions,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        ILogger<ImportGoldModelActivity> logger)
    {
        _databaseOptions = databaseOptions;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _logger = logger;
    }

    [Function(nameof(ImportGoldModelActivity))]
    public async Task RunAsync([ActivityTrigger] ImportGoldModelActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var sw = Stopwatch.StartNew();

        await ImportAsync(input.Cutoff).ConfigureAwait(false);

        _logger.LogWarning("Gold model imported in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        _importCollection.Dispose();
        _submitCollection.Dispose();
    }

    private static KeyValuePair<string, Action<object?, ImportedTransactionEntity>> MapProperty<TProp>(
        Expression<Func<ImportedTransactionEntity, TProp>> propertyExpression,
        TProp? defaultValue = default)
    {
        if (propertyExpression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("The expression must be a property access expression.", nameof(propertyExpression));

        if (memberExpression.Member is not PropertyInfo propertyInfo)
            throw new ArgumentException("The expression must point to a property.", nameof(propertyExpression));

        var target = Expression.Parameter(typeof(ImportedTransactionEntity), "target");
        var value = Expression.Parameter(typeof(object), "value");

        Expression convertedValue;

        if (!Equals(defaultValue, default(TProp)))
        {
            convertedValue = Expression.Condition(
                Expression.Equal(value, Expression.Constant(null)),
                Expression.Constant(defaultValue, propertyInfo.PropertyType),
                Expression.Convert(value, propertyInfo.PropertyType));
        }
        else
        {
            convertedValue = Expression.Convert(value, propertyInfo.PropertyType);
        }

        var body = Expression.Assign(Expression.Property(target, propertyInfo), convertedValue);
        var compiled = Expression.Lambda<Action<object?, ImportedTransactionEntity>>(body, value, target).Compile();

        return new KeyValuePair<string, Action<object?, ImportedTransactionEntity>>(propertyInfo.Name, compiled);
    }

    private async Task ImportAsync(long cutoff)
    {
        var importSilver = Task.Run(async () =>
        {
            try
            {
                await ImportDataAsync(cutoff).ConfigureAwait(false);
            }
            catch
            {
                _importCollection.Dispose();
                throw;
            }
        });

        var goldTransform = Task.Run(() =>
        {
            try
            {
                PackageRecords();
            }
            catch
            {
                _submitCollection.Dispose();
                throw;
            }
        });

        var bulkInsert = Task.Run(BulkInsertAsync);

        await Task.WhenAll(importSilver, goldTransform, bulkInsert).ConfigureAwait(false);
    }

    private async Task ImportDataAsync(long cutoff)
    {
        var query = DatabricksStatement.FromRawSql(
            $"""
             SELECT
                CAST(metering_point_id AS BIGINT) AS metering_point_id,
                valid_from_date,
                valid_to_date,
                CAST(dh2_created AS TIMESTAMP) AS dh2_created,
                metering_grid_area_id,
                metering_point_state_id,
                btd_trans_doss_id,
                parent_metering_point_id,
                type_of_mp,
                sub_type_of_mp,
                physical_status_of_mp,
                web_access_code,
                balance_supplier_id,
                effectuation_date,
                transaction_type,
                meter_reading_occurrence,
                mp_connection_type,
                disconnection_type,
                product,
                CAST(product_obligation AS BOOLEAN) AS product_obligation,
                energy_timeseries_measure_unit,
                asset_type,
                CAST(fuel_type AS BOOLEAN) AS fuel_type,
                mp_capacity,
                CAST(power_limit_kw AS INT) AS power_limit_kw,
                CAST(power_limit_a AS INT) AS power_limit_a,
                meter_number,
                CAST(net_settlement_group AS INT) AS net_settlement_group,
                scheduled_meter_reading_date01,
                from_grid_area,
                to_grid_area,
                power_plant_gsrn,
                settlement_method,
                location_street_code,
                location_street_name,
                location_building_number,
                location_city_name,
                location_city_subdivision_name,
                location_dar_reference,
                location_country_name,
                location_floor_id,
                location_room_id,
                location_postcode,
                location_municipality_code,
                location_location_description,
                first_consumer_party_name,
                second_consumer_party_name,
                consumer_cvr,
                CAST(protected_name AS BOOLEAN) AS protected_name,
                contact_1_contact_name1,
                CAST(contact_1_protected_address AS BOOLEAN) AS contact_1_protected_address,
                contact_1_phone_number,
                contact_1_mobile_number,
                contact_1_email_address,
                contact_1_attention,
                contact_1_street_code,
                contact_1_street_name,
                contact_1_building_number,
                contact_1_postcode,
                contact_1_city_name,
                contact_1_city_subdivision_name,
                contact_1_dar_reference,
                contact_1_country_name,
                contact_1_floor_id,
                contact_1_room_id,
                contact_1_post_box,
                contact_1_municipality_code,
                contact_4_contact_name1,
                CAST(contact_4_protected_address AS BOOLEAN) AS contact_4_protected_address,
                contact_4_phone_number,
                contact_4_mobile_number,
                contact_4_email_address,
                contact_4_attention,
                contact_4_street_code,
                contact_4_street_name,
                contact_4_building_number,
                contact_4_postcode,
                contact_4_city_name,
                contact_4_city_subdivision_name,
                contact_4_dar_reference,
                contact_4_country_name,
                contact_4_floor_id,
                contact_4_room_id,
                contact_4_post_box,
                contact_4_municipality_code
             
             FROM migrations_electricity_market.electricity_market_metering_points_view_v3
             WHERE btd_trans_doss_id < {cutoff}
             """);

        var sw = Stopwatch.StartNew();
        var firstLog = true;

        var results = _databricksSqlWarehouseQueryExecutor
            .ExecuteStatementAsync(query.Build())
            .ConfigureAwait(false);

        await foreach (var record in results)
        {
            if (firstLog)
            {
                _logger.LogWarning("Databricks first result after {FirstResult} ms.", sw.ElapsedMilliseconds);
                firstLog = false;
            }

            _importCollection.Add(record);
        }

        _logger.LogWarning("All databricks results added after {DatabricksCompleted} ms.", sw.ElapsedMilliseconds);
        _importCollection.CompleteAdding();
    }

    private void PackageRecords()
    {
        var lookup = _importFields.ToImmutableDictionary(k => k.Key, v => v.Value);

        const int capacity = 50000;

        var sw = Stopwatch.StartNew();
        var batch = new List<ImportedTransactionEntity>(capacity);

        var columnOrder = _importFields
            .Select(f => f.Key)
            .Prepend("Id")
            .ToArray();

        foreach (var record in _importCollection.GetConsumingEnumerable())
        {
            if (batch.Count == capacity)
            {
                _submitCollection.Add(ObjectReader.Create(batch, columnOrder));
                _logger.LogWarning("A batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

                sw = Stopwatch.StartNew();
                batch = new List<ImportedTransactionEntity>(capacity);
            }

            var importedTransaction = new ImportedTransactionEntity();

            foreach (var keyValuePair in record)
            {
                lookup[keyValuePair.Key](keyValuePair.Value, importedTransaction);
            }

            batch.Add(importedTransaction);
        }

        _submitCollection.Add(ObjectReader.Create(batch, columnOrder));
        _logger.LogWarning("Final batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

        _submitCollection.CompleteAdding();
    }

    private async Task BulkInsertAsync()
    {
        using var bulkCopy = new SqlBulkCopy(
            _databaseOptions.Value.ConnectionString,
            SqlBulkCopyOptions.TableLock);

        bulkCopy.DestinationTableName = "electricitymarket.GoldenImport";
        bulkCopy.BulkCopyTimeout = 0;

        foreach (var batch in _submitCollection.GetConsumingEnumerable())
        {
            var sw = Stopwatch.StartNew();

            await bulkCopy.WriteToServerAsync(batch).ConfigureAwait(false);
            batch.Dispose();

            _logger.LogWarning("A batch was inserted in {InsertTime} ms.", sw.ElapsedMilliseconds);
        }
    }
}
