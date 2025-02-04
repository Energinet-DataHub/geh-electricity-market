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

    public string GridAreaCode { get; set; } = null!;

    public string OwnedBy { get; set; } = null!;

    public string ConnectionState { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string SubType { get; set; } = null!;

    public string Resolution { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public string ProductId { get; set; } = null!;

    public int ScheduledMeterReadingMonth { get; set; }

    public long MeteringPointStateId { get; set; }

    public long BusinessTransactionDosId { get; set; }

    public long? RetiredById { get; set; }

    public DateTimeOffset? RetiredAt { get; set; }

    public MeteringPointPeriodEntity? RetiredBy { get; set; }
    public string? ParentIdentification { get; internal set; }
}
