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

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using ElectricityMarket.ImportOrchestrator.Orchestration.Activities;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Helpers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectricityMarket.ImportOrchestrator.Services;

public sealed class DatabricksStreamingImporter : IDatabricksStreamingImporter
{
    private readonly IOptions<DatabricksCatalogOptions> _catalogOptions;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly FindCutoffActivity _findCutoffActivity;
    private readonly ElectricityMarketDatabaseContext _databaseContext;
    private readonly IImportStateService _importStateService;
    private readonly IStreamingImporter _streamingImporter;
    private readonly ILogger<DatabricksStreamingImporter> _logger;

    public DatabricksStreamingImporter(
        IOptions<DatabricksCatalogOptions> catalogOptions,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        FindCutoffActivity findCutoffActivity,
        ElectricityMarketDatabaseContext databaseContext,
        IImportStateService importStateService,
        IStreamingImporter streamingImporter,
        ILogger<DatabricksStreamingImporter> logger)
    {
        _catalogOptions = catalogOptions;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _findCutoffActivity = findCutoffActivity;
        _databaseContext = databaseContext;
        _importStateService = importStateService;
        _streamingImporter = streamingImporter;
        _logger = logger;
    }

    public async Task ImportAsync()
    {
        var sw = Stopwatch.StartNew();

        var currentMaxCutoff = await _findCutoffActivity
            .RunAsync(new NoInput())
            .ConfigureAwait(false);

        _logger.LogWarning($"databricks-streaming-importer: current max cutoff fetched in {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        var previousCutoff = await _importStateService
            .GetStreamingImportCutoffAsync()
            .ConfigureAwait(false);

        _logger.LogWarning($"databricks-streaming-importer: current prev cutoff fetched in {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        if (currentMaxCutoff == previousCutoff)
            return;

        var targetCutoff = Math.Min(currentMaxCutoff, previousCutoff + 8_000);

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
                CAST(power_limit_kw AS DECIMAL(11, 1)) AS power_limit_kw,
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
                CASE WHEN first_consumer_cpr IS NOT NULL AND length(first_consumer_cpr) > 0 THEN '1111110000' ELSE first_consumer_cpr END AS first_consumer_cpr,
                second_consumer_party_name,
                CASE WHEN second_consumer_cpr IS NOT NULL AND length(second_consumer_cpr) > 0 THEN '1111110000' ELSE second_consumer_cpr END AS second_consumer_cpr,
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
                contact_4_municipality_code,
                dossier_status,
                CAST(tax_reduction AS BOOLEAN) AS tax_reduction,
                CAST(tax_settlement_date AS TIMESTAMP) AS tax_settlement_date

             FROM {_catalogOptions.Value.Name}.migrations_electricity_market.electricity_market_metering_points_view_v5
             WHERE btd_trans_doss_id >= {previousCutoff} AND btd_trans_doss_id < {targetCutoff}
             """);

        var results = _databricksSqlWarehouseQueryExecutor
            .ExecuteStatementAsync(query.Build())
            .ConfigureAwait(false);

        var transaction = await _databaseContext.Database
            .BeginTransactionAsync()
            .ConfigureAwait(false);

        await using (transaction.ConfigureAwait(false))
        {
            var lookup = ImportModelHelper
                .ImportFields
                .ToImmutableDictionary(k => k.Key, v => v.Value);

            var i = 0;
            long goldenMin = 0, goldenMax = 0, goldenTotal = 0;
            long relMin = 0, relMax = 0, relTotal = 0;
            long saveMin = 0, saveMax = 0, saveTotal = 0;
            long min = 0, max = 0, total = 0;

            await foreach (var record in results)
            {
                ++i;
                sw.Restart();
                var importedTransaction = new ImportedTransactionEntity();

                foreach (var keyValuePair in record)
                {
                    lookup[keyValuePair.Key](keyValuePair.Value, importedTransaction);
                }

                var sql = $"""
                            INSERT INTO [electricitymarket].[GoldenImport]
                            (metering_point_id, valid_from_date, valid_to_date, dh2_created, metering_grid_area_id, metering_point_state_id, btd_trans_doss_id, parent_metering_point_id, type_of_mp, sub_type_of_mp, physical_status_of_mp, web_access_code, balance_supplier_id, transaction_type, meter_reading_occurrence, mp_connection_type, disconnection_type, product, product_obligation, energy_timeseries_measure_unit, asset_type, fuel_type, mp_capacity, power_limit_kw, power_limit_a, meter_number, net_settlement_group, scheduled_meter_reading_date01, from_grid_area, to_grid_area, power_plant_gsrn, settlement_method, location_street_code, location_street_name, location_building_number, location_city_name, location_city_subdivision_name, location_dar_reference, location_mp_address_wash_instructions, location_country_name, location_floor_id, location_room_id, location_postcode, location_municipality_code, location_location_description, first_consumer_party_name, first_consumer_cpr, second_consumer_party_name, second_consumer_cpr, consumer_cvr, protected_name, contact_1_contact_name1, contact_1_protected_address, contact_1_phone_number, contact_1_mobile_number, contact_1_email_address, contact_1_attention, contact_1_street_code, contact_1_street_name, contact_1_building_number, contact_1_postcode, contact_1_city_name, contact_1_city_subdivision_name, contact_1_dar_reference, contact_1_country_name, contact_1_floor_id, contact_1_room_id, contact_1_post_box, contact_1_municipality_code, contact_4_contact_name1, contact_4_protected_address, contact_4_phone_number, contact_4_mobile_number, contact_4_email_address, contact_4_attention, contact_4_street_code, contact_4_street_name, contact_4_building_number, contact_4_postcode, contact_4_city_name, contact_4_city_subdivision_name, contact_4_dar_reference, contact_4_country_name, contact_4_floor_id, contact_4_room_id, contact_4_post_box, contact_4_municipality_code, dossier_status, tax_reduction, tax_settlement_date)
                            VALUES
                            (
                            {importedTransaction.metering_point_id},
                            '{importedTransaction.valid_from_date:O}',
                            '{importedTransaction.valid_to_date:O}',
                            '{importedTransaction.dh2_created:O}',
                            '{importedTransaction.metering_grid_area_id.Replace("'", "''", StringComparison.InvariantCulture)}',
                            {importedTransaction.metering_point_state_id},
                            {importedTransaction.btd_trans_doss_id},
                            {(importedTransaction.parent_metering_point_id == null ? "NULL" : $"'{importedTransaction.parent_metering_point_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            '{importedTransaction.type_of_mp.Replace("'", "''", StringComparison.InvariantCulture)}',
                            '{importedTransaction.sub_type_of_mp.Replace("'", "''", StringComparison.InvariantCulture)}',
                            '{importedTransaction.physical_status_of_mp.Replace("'", "''", StringComparison.InvariantCulture)}',
                            {(importedTransaction.web_access_code == null ? "NULL" : $"'{importedTransaction.web_access_code.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.balance_supplier_id == null ? "NULL" : $"'{importedTransaction.balance_supplier_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            '{importedTransaction.transaction_type.Replace("'", "''", StringComparison.InvariantCulture)}',
                            '{importedTransaction.meter_reading_occurrence.Replace("'", "''", StringComparison.InvariantCulture)}',
                            {(importedTransaction.mp_connection_type == null ? "NULL" : $"'{importedTransaction.mp_connection_type.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.disconnection_type == null ? "NULL" : $"'{importedTransaction.disconnection_type.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            '{importedTransaction.product.Replace("'", "''", StringComparison.InvariantCulture)}',
                            {(importedTransaction.product_obligation == null ? "NULL" : importedTransaction.product_obligation.Value ? "1" : "0")},
                            '{importedTransaction.energy_timeseries_measure_unit.Replace("'", "''", StringComparison.InvariantCulture)}',
                            {(importedTransaction.asset_type == null ? "NULL" : $"'{importedTransaction.asset_type.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.fuel_type == null ? "NULL" : importedTransaction.fuel_type.Value ? "1" : "0")},
                            {(importedTransaction.mp_capacity == null ? "NULL" : $"'{importedTransaction.mp_capacity.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.power_limit_kw == null ? "NULL" : importedTransaction.power_limit_kw.Value.ToString(CultureInfo.InvariantCulture))},
                            {(importedTransaction.power_limit_a == null ? "NULL" : importedTransaction.power_limit_a.Value.ToString(CultureInfo.InvariantCulture))},
                            {(importedTransaction.meter_number == null ? "NULL" : $"'{importedTransaction.meter_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.net_settlement_group == null ? "NULL" : importedTransaction.net_settlement_group.Value.ToString(CultureInfo.InvariantCulture))},
                            {(importedTransaction.scheduled_meter_reading_date01 == null ? "NULL" : $"'{importedTransaction.scheduled_meter_reading_date01.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.from_grid_area == null ? "NULL" : $"'{importedTransaction.from_grid_area.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.to_grid_area == null ? "NULL" : $"'{importedTransaction.to_grid_area.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.power_plant_gsrn == null ? "NULL" : $"'{importedTransaction.power_plant_gsrn.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.settlement_method == null ? "NULL" : $"'{importedTransaction.settlement_method.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_street_code == null ? "NULL" : $"'{importedTransaction.location_street_code.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_street_name == null ? "NULL" : $"'{importedTransaction.location_street_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_building_number == null ? "NULL" : $"'{importedTransaction.location_building_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_city_name == null ? "NULL" : $"'{importedTransaction.location_city_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_city_subdivision_name == null ? "NULL" : $"'{importedTransaction.location_city_subdivision_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_dar_reference == null ? "NULL" : $"'{importedTransaction.location_dar_reference.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_mp_address_wash_instructions == null ? "NULL" : $"'{importedTransaction.location_mp_address_wash_instructions.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_country_name == null ? "NULL" : $"'{importedTransaction.location_country_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_floor_id == null ? "NULL" : $"'{importedTransaction.location_floor_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_room_id == null ? "NULL" : $"'{importedTransaction.location_room_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_postcode == null ? "NULL" : $"'{importedTransaction.location_postcode.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_municipality_code == null ? "NULL" : $"'{importedTransaction.location_municipality_code.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.location_location_description == null ? "NULL" : $"'{importedTransaction.location_location_description.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.first_consumer_party_name == null ? "NULL" : $"'{importedTransaction.first_consumer_party_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.first_consumer_cpr == null ? "NULL" : $"'{importedTransaction.first_consumer_cpr.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.second_consumer_party_name == null ? "NULL" : $"'{importedTransaction.second_consumer_party_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.second_consumer_cpr == null ? "NULL" : $"'{importedTransaction.second_consumer_cpr.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.consumer_cvr == null ? "NULL" : $"'{importedTransaction.consumer_cvr.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.protected_name == null ? "NULL" : importedTransaction.protected_name.Value ? "1" : "0")},
                            {(importedTransaction.contact_1_contact_name1 == null ? "NULL" : $"'{importedTransaction.contact_1_contact_name1.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_protected_address == null ? "NULL" : importedTransaction.contact_1_protected_address.Value ? "1" : "0")},
                            {(importedTransaction.contact_1_phone_number == null ? "NULL" : $"'{importedTransaction.contact_1_phone_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_mobile_number == null ? "NULL" : $"'{importedTransaction.contact_1_mobile_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_email_address == null ? "NULL" : $"'{importedTransaction.contact_1_email_address.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_attention == null ? "NULL" : $"'{importedTransaction.contact_1_attention.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_street_code == null ? "NULL" : $"'{importedTransaction.contact_1_street_code.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_street_name == null ? "NULL" : $"'{importedTransaction.contact_1_street_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_building_number == null ? "NULL" : $"'{importedTransaction.contact_1_building_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_postcode == null ? "NULL" : $"'{importedTransaction.contact_1_postcode.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_city_name == null ? "NULL" : $"'{importedTransaction.contact_1_city_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_city_subdivision_name == null ? "NULL" : $"'{importedTransaction.contact_1_city_subdivision_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_dar_reference == null ? "NULL" : $"'{importedTransaction.contact_1_dar_reference.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_country_name == null ? "NULL" : $"'{importedTransaction.contact_1_country_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_floor_id == null ? "NULL" : $"'{importedTransaction.contact_1_floor_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_room_id == null ? "NULL" : $"'{importedTransaction.contact_1_room_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_post_box == null ? "NULL" : $"'{importedTransaction.contact_1_post_box.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_1_municipality_code == null ? "NULL" : $"'{importedTransaction.contact_1_municipality_code.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_contact_name1 == null ? "NULL" : $"'{importedTransaction.contact_4_contact_name1.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_protected_address == null ? "NULL" : importedTransaction.contact_4_protected_address.Value ? "1" : "0")},
                            {(importedTransaction.contact_4_phone_number == null ? "NULL" : $"'{importedTransaction.contact_4_phone_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_mobile_number == null ? "NULL" : $"'{importedTransaction.contact_4_mobile_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_email_address == null ? "NULL" : $"'{importedTransaction.contact_4_email_address.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_attention == null ? "NULL" : $"'{importedTransaction.contact_4_attention.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_street_code == null ? "NULL" : $"'{importedTransaction.contact_4_street_code.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_street_name == null ? "NULL" : $"'{importedTransaction.contact_4_street_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_building_number == null ? "NULL" : $"'{importedTransaction.contact_4_building_number.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_postcode == null ? "NULL" : $"'{importedTransaction.contact_4_postcode.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_city_name == null ? "NULL" : $"'{importedTransaction.contact_4_city_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_city_subdivision_name == null ? "NULL" : $"'{importedTransaction.contact_4_city_subdivision_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_dar_reference == null ? "NULL" : $"'{importedTransaction.contact_4_dar_reference.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_country_name == null ? "NULL" : $"'{importedTransaction.contact_4_country_name.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_floor_id == null ? "NULL" : $"'{importedTransaction.contact_4_floor_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_room_id == null ? "NULL" : $"'{importedTransaction.contact_4_room_id.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_post_box == null ? "NULL" : $"'{importedTransaction.contact_4_post_box.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.contact_4_municipality_code == null ? "NULL" : $"'{importedTransaction.contact_4_municipality_code.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.dossier_status == null ? "NULL" : $"'{importedTransaction.dossier_status.Replace("'", "''", StringComparison.InvariantCulture)}'")},
                            {(importedTransaction.tax_reduction == null ? "NULL" : importedTransaction.tax_reduction.Value ? "1" : "0")},
                            {(importedTransaction.tax_settlement_date == null ? "NULL" : $"'{importedTransaction.tax_settlement_date.Value:O}'")}
                            )
                            """;

                await _databaseContext.Database
#pragma warning disable EF1002
                    .ExecuteSqlRawAsync(sql)
#pragma warning restore EF1002
                    .ConfigureAwait(false);

                sw.Stop();
                goldenMin = Math.Min(goldenMin, sw.ElapsedMilliseconds);
                goldenMax = Math.Max(goldenMax, sw.ElapsedMilliseconds);
                goldenTotal += sw.ElapsedMilliseconds;

                sw.Restart();
                await _streamingImporter.ImportAsync(importedTransaction).ConfigureAwait(false);

                sw.Stop();
                relMin = Math.Min(relMin, sw.ElapsedMilliseconds);
                relMax = Math.Max(relMax, sw.ElapsedMilliseconds);
                relTotal += sw.ElapsedMilliseconds;

                sw.Restart();
                await _databaseContext.SaveChangesAsync().ConfigureAwait(false);

                sw.Stop();
                saveMin = Math.Min(saveMin, sw.ElapsedMilliseconds);
                saveMax = Math.Max(saveMax, sw.ElapsedMilliseconds);
                saveTotal += sw.ElapsedMilliseconds;

                min = Math.Min(min, goldenMin + relMin + saveMin);
                max = Math.Max(max, goldenMax + relMax + saveMax);
                total += goldenTotal + relTotal + saveTotal;
            }

            _logger.LogWarning($"databricks-streaming-importer: gold time. Min: {goldenMin}ms. Max: {goldenMax}ms. Avg: {(i > 0 ? goldenTotal / i : 0)}ms.");
            _logger.LogWarning($"databricks-streaming-importer: rel time. Min: {relMin}ms. Max: {relMax}ms. Avg: {(i > 0 ? relTotal / i : 0)}ms.");
            _logger.LogWarning($"databricks-streaming-importer: save time. Min: {saveMin}ms. Max: {saveMax}ms. Avg: {(i > 0 ? saveTotal / i : 0)}ms.");
            _logger.LogWarning($"databricks-streaming-importer: total time. Min: {min}ms. Max: {max}ms. Avg: {(i > 0 ? total / i : 0)}ms.");
            sw.Restart();

            await _importStateService
                .UpdateStreamingCutoffAsync(targetCutoff)
                .ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);

            _logger.LogWarning($"databricks-streaming-importer: transaction committed in {sw.ElapsedMilliseconds}ms");
        }
    }
}
