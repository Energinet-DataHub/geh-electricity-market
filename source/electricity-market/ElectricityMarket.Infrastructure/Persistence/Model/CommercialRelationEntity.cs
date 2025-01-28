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
using System.Collections.Generic;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

public sealed class CommercialRelationEntity
{
    public long Id { get; set; }
    public string CustomerId { get; set; } = null!;

    public long MeteringPointId { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public string EnergySupplier { get; set; } = null!;

    public DateTimeOffset ModifiedAt { get; set; }
    public ICollection<EnergyPeriodEntity> EnergyPeriods { get; init; } = [];
}
