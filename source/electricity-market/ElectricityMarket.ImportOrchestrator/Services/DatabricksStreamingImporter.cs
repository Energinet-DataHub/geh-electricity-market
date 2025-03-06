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
using ElectricityMarket.ImportOrchestrator.Orchestration.Activities;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Helpers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

namespace ElectricityMarket.ImportOrchestrator.Services;

public sealed class DatabricksStreamingImporter : IDatabricksStreamingImporter
{
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly FindCutoffActivity _findCutoffActivity;
    private readonly ElectricityMarketDatabaseContext _databaseContext;
    private readonly IImportStateService _importStateService;
    private readonly IStreamingImporter _streamingImporter;

    public DatabricksStreamingImporter(
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        FindCutoffActivity findCutoffActivity,
        ElectricityMarketDatabaseContext databaseContext,
        IImportStateService importStateService,
        IStreamingImporter streamingImporter)
    {
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _findCutoffActivity = findCutoffActivity;
        _databaseContext = databaseContext;
        _importStateService = importStateService;
        _streamingImporter = streamingImporter;
    }

    public async Task ImportAsync()
    {
        var currentMaxCutoff = await _findCutoffActivity
            .RunAsync(new NoInput())
            .ConfigureAwait(false);

        var previousCutoff = await _importStateService
            .GetStreamingImportCutoffAsync()
            .ConfigureAwait(false);

        if (currentMaxCutoff == previousCutoff)
            return;

        var transaction = await _databaseContext.Database
            .BeginTransactionAsync()
            .ConfigureAwait(false);

        await using (transaction.ConfigureAwait(false))
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
                first_consumer_cpr,
                second_consumer_party_name,
                second_consumer_cpr,
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
                dossier_status

             FROM migrations_electricity_market.electricity_market_metering_points_view_v3
             WHERE btd_trans_doss_id >= {previousCutoff} AND btd_trans_doss_id < {currentMaxCutoff}
             """);

            var results = _databricksSqlWarehouseQueryExecutor
                .ExecuteStatementAsync(query.Build())
                .ConfigureAwait(false);

            var lookup = ImportModelHelper
                .ImportFields
                .ToImmutableDictionary(k => k.Key, v => v.Value);

            await foreach (var record in results)
            {
                var importedTransaction = new ImportedTransactionEntity();

                foreach (var keyValuePair in record)
                {
                    lookup[keyValuePair.Key](keyValuePair.Value, importedTransaction);
                }

                await _databaseContext.ImportedTransactions
                    .AddAsync(importedTransaction)
                    .ConfigureAwait(false);

                await _streamingImporter.ImportAsync(importedTransaction).ConfigureAwait(false);

                await _databaseContext.SaveChangesAsync().ConfigureAwait(false);
            }

            await _importStateService
                .UpdateStreamingCutoffAsync(currentMaxCutoff)
                .ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
        }
    }
}
