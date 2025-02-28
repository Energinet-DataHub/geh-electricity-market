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

using CsvHelper.Configuration.Attributes;

namespace Energinet.DataHub.ElectricityMarket.Application.Models;

public sealed class ImportedTransactionRecord
{
    [Name("metering_point_id")]
    public long MeteringPointId { get; set; }
    [Name("valid_from_date")]
    public DateTimeOffset ValidFromDate { get; set; }
    [Name("valid_to_date")]
    public DateTimeOffset? ValidToDate { get; set; }
    [Name("dh2_created")]
    public DateTimeOffset DH2CreatedDate { get; set; }
    [Name("metering_grid_area_id")]
    public string MeteringGridAreaId { get; set; } = null!;
    [Name("metering_point_state_id")]
    public long MeteringPointStateId { get; set; }
    [Name("btd_business_trans_doss_id")]
    public long? BusinessTransactionDosId { get; set; }
    [Name("physical_status_of_mp")]
    public string PhysicalStatus { get; set; } = null!;
    [Name("type_of_mp")]
    public string Type { get; set; } = null!;
    [Name("sub_type_of_mp")]
    public string SubType { get; set; } = null!;
    [Name("energy_timeseries_measure_unit")]
    public string MeasureUnit { get; set; } = null!;
    [Name("web_access_code")]
    public string? WebAccessCode { get; set; }
    [Name("balance_supplier_id")]
    public string? BalanceSupplierId { get; set; }
    [Name("effectuation_date")]
    public DateTimeOffset? EffectuationDate { get; set; }
    [Name("transaction_type")]
    public string? TransactionType { get; set; } = null!;
}
