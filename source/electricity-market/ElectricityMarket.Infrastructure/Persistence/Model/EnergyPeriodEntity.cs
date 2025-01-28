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
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

public sealed class EnergyPeriodEntity
{
    public long Id { get; set; }

    public long CommercialRelationId { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidTo { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
    public long? RetiredById { get; set; }
    public EnergyPeriodEntity? RetiredBy { get; set; }
    public long BusinessTransactionDosId { get; set; }
    public string WebAccessCode { get; set; } = null!;
    public string EnergySupplier { get; set; } = null!;
}
