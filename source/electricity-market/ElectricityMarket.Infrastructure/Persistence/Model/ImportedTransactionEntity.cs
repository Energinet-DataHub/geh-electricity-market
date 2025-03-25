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

#pragma warning disable SA1300, CA1707

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

public sealed class ImportedTransactionEntity
{
    public long metering_point_id { get; set; }
    public DateTimeOffset valid_from_date { get; set; }
    public DateTimeOffset valid_to_date { get; set; }
    public DateTimeOffset dh2_created { get; set; }
    public string metering_grid_area_id { get; set; } = null!;
    public long metering_point_state_id { get; set; }
    public long btd_trans_doss_id { get; set; }
    public string? parent_metering_point_id { get; set; }
    public string type_of_mp { get; set; } = null!;
    public string sub_type_of_mp { get; set; } = null!;
    public string physical_status_of_mp { get; set; } = null!;
    public string? web_access_code { get; set; }
    public string? balance_supplier_id { get; set; }
    public DateTimeOffset effectuation_date { get; set; }
    public string transaction_type { get; set; } = null!;
    public string meter_reading_occurrence { get; set; } = null!;
    public string? mp_connection_type { get; set; }
    public string? disconnection_type { get; set; }
    public string product { get; set; } = null!;
    public bool? product_obligation { get; set; }
    public string energy_timeseries_measure_unit { get; set; } = null!;
    public string? asset_type { get; set; }
    public bool? fuel_type { get; set; }
    public string? mp_capacity { get; set; }
    public int? power_limit_kw { get; set; }
    public int? power_limit_a { get; set; }
    public string? meter_number { get; set; }
    public int? net_settlement_group { get; set; }
    public string? scheduled_meter_reading_date01 { get; set; }
    public string? from_grid_area { get; set; }
    public string? to_grid_area { get; set; }
    public string? power_plant_gsrn { get; set; }
    public string? settlement_method { get; set; }
    public string? location_street_code { get; set; }
    public string? location_street_name { get; set; }
    public string? location_building_number { get; set; }
    public string? location_city_name { get; set; }
    public string? location_city_subdivision_name { get; set; }
    public string? location_dar_reference { get; set; }
    public string? location_country_name { get; set; }
    public string? location_floor_id { get; set; }
    public string? location_room_id { get; set; }
    public string? location_postcode { get; set; }
    public string? location_municipality_code { get; set; }
    public string? location_location_description { get; set; }
    public string? first_consumer_party_name { get; set; }
    public string? first_consumer_cpr { get; set; }
    public string? second_consumer_party_name { get; set; }
    public string? second_consumer_cpr { get; set; }
    public string? consumer_cvr { get; set; }
    public bool? protected_name { get; set; }
    public string? contact_1_contact_name1 { get; set; }
    public bool? contact_1_protected_address { get; set; }
    public string? contact_1_phone_number { get; set; }
    public string? contact_1_mobile_number { get; set; }
    public string? contact_1_email_address { get; set; }
    public string? contact_1_attention { get; set; }
    public string? contact_1_street_code { get; set; }
    public string? contact_1_street_name { get; set; }
    public string? contact_1_building_number { get; set; }
    public string? contact_1_postcode { get; set; }
    public string? contact_1_city_name { get; set; }
    public string? contact_1_city_subdivision_name { get; set; }
    public string? contact_1_dar_reference { get; set; }
    public string? contact_1_country_name { get; set; }
    public string? contact_1_floor_id { get; set; }
    public string? contact_1_room_id { get; set; }
    public string? contact_1_post_box { get; set; }
    public string? contact_1_municipality_code { get; set; }
    public string? contact_4_contact_name1 { get; set; }
    public bool? contact_4_protected_address { get; set; }
    public string? contact_4_phone_number { get; set; }
    public string? contact_4_mobile_number { get; set; }
    public string? contact_4_email_address { get; set; }
    public string? contact_4_attention { get; set; }
    public string? contact_4_street_code { get; set; }
    public string? contact_4_street_name { get; set; }
    public string? contact_4_building_number { get; set; }
    public string? contact_4_postcode { get; set; }
    public string? contact_4_city_name { get; set; }
    public string? contact_4_city_subdivision_name { get; set; }
    public string? contact_4_dar_reference { get; set; }
    public string? contact_4_country_name { get; set; }
    public string? contact_4_floor_id { get; set; }
    public string? contact_4_room_id { get; set; }
    public string? contact_4_post_box { get; set; }
    public string? contact_4_municipality_code { get; set; }
    public string? dossier_status { get; set; }
}
