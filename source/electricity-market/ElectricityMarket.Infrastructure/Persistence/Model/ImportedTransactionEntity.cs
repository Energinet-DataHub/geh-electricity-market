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
    public int Id { get; set; }
    public long metering_point_id { get; set; }
    public DateTimeOffset valid_from_date { get; set; }
    public DateTimeOffset valid_to_date { get; set; }
    public DateTimeOffset dh2_created { get; set; }
    public string metering_grid_area_id { get; set; } = null!;
    public long metering_point_state_id { get; set; }
    public long btd_trans_doss_id { get; set; }
    public string physical_status_of_mp { get; set; } = null!;
    public string type_of_mp { get; set; } = null!;
    public string sub_type_of_mp { get; set; } = null!;
    public string energy_timeseries_measure_unit { get; set; } = null!;
    public string? web_access_code { get; set; }
    public string? balance_supplier_id { get; set; }
    public DateTimeOffset effectuation_date { get; set; }
    public string transaction_type { get; set; } = null!;
}
