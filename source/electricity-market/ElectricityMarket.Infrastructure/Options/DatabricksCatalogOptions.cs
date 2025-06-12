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

using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Options;

public sealed record DatabricksCatalogOptions
{
    public const string SectionName = "DatabricksCatalog";

    [Required]
    public string Name { get; set; } = null!;

    public string SchemaName { get; } = "electricity_market_internal";

    public string ElectricalHeatingParentTableName { get; } = "electrical_heating_consumption_metering_point_periods";
    public string ElectricalHeatingChildTableName { get; } = "electrical_heating_child_metering_points";
    public string MissingMeasurementLogsTableName { get; } = "missing_measurements_log_metering_point_periods";
    public string NetConsumptionParentTableName { get; } = "net_consumption_group_6_consumption_metering_point_periods";
    public string NetConsumptionChildTableName { get; } = "net_consumption_group_6_child_metering_point";
    public string CapacitySettlementPeriodTableName { get; } = "capacity_settlement_metering_point_periods";
    public string MeasurementsReportTableName { get; } = "measurements_report_metering_point_periods";
}
