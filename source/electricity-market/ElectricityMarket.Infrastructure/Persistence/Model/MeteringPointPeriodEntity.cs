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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

public sealed class MeteringPointPeriodEntity
{
    public long Id { get; set; }
    public long MeteringPointId { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? ParentIdentification { get; set; }

    public long? RetiredById { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
    public MeteringPointPeriodEntity? RetiredBy { get; set; }

    public string Type { get; set; } = null!;
    public string SubType { get; set; } = null!;
    public string ConnectionState { get; set; } = null!;
    public string Resolution { get; set; } = null!;
    public string GridAreaCode { get; set; } = null!;
    public string? OwnedBy { get; set; }
    public string? ConnectionType { get; set; }
    public string? DisconnectionType { get; set; }
    public string Product { get; set; } = null!;
    public bool? ProductObligation { get; set; }
    public string MeasureUnit { get; set; } = null!;
    public string? AssetType { get; set; }
    public bool? FuelType { get; set; }
    public string? Capacity { get; set; }
    public decimal? PowerLimitKw { get; set; }
    public int? PowerLimitA { get; set; }
    public string? MeterNumber { get; set; }
    public int? SettlementGroup { get; set; }
    public int? ScheduledMeterReadingMonth { get; set; }
    public string? ExchangeFromGridArea { get; set; }
    public string? ExchangeToGridArea { get; set; }
    public string? PowerPlantGsrn { get; set; }
    public string? SettlementMethod { get; set; }
    public long InstallationAddressId { get; set; }

    public long MeteringPointStateId { get; set; }
    public long BusinessTransactionDosId { get; set; }
    public string TransactionType { get; set; } = null!;

    public InstallationAddressEntity InstallationAddress { get; set; } = null!;
}
