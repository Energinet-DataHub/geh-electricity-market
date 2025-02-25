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

namespace Energinet.DataHub.ElectricityMarket.Application.Models;

public sealed record MeteringPointMetadataDto(
    long Id,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    string? ParentMeteringPoint, // parent_metering_point_id
    MeteringPointType Type, // type_of_mp
    MeteringPointSubType SubType, // sub_type_of_mp
    ConnectionState ConnectionState, // physical_status_of_mp
    string Resolution, // meter_reading_occurrence
    string GridAreaCode, // metering_grid_area_id
    string OwnedBy, // Computed
    ConnectionType? ConnectionType, // mp_connection_type
    DisconnectionType? DisconnectionType, // disconnection_type
    Product Product, // product
    bool? ProductObligation, // product_obligation // Aftalepligt
    MeteringPointMeasureUnit MeasureUnit, // energy_timeseries_measure_unit
    AssetType? AssetType, // asset_type
    bool? FuelType, // fuel_type
    string? Capacity, // mp_capacity
    int? PowerLimitKw, // power_limit_kw
    int? PowerLimitA, // power_limit_a TODO: Ampere?
    string? MeterNumber, // meter_number
    int? NetSettlementGroup, // net_settlement_group
    int? ScheduledMeterReadingMonth, // scheduled_meter_reading_date01,
    string? FromGridAreaCode, // from_grid_area
    string? ToGridAreaCode, // to_grid_area
    string? PowerPlantGsrn, // power_plant_gsrn
    SettlementMethod? SettlementMethod, // settlement_method
    InstallationAddressDto InstallationAddress);
