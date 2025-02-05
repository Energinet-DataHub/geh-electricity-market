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

using System.Collections.Generic;
using ElectricityMarket.Domain.Models.MasterData;
using NodaTime;

namespace ElectricityMarket.Domain.Models.Common;

public sealed class MeteringPointMasterData
{
    public MeteringPointIdentification Identification { get; set; } = null!;

    public Instant ValidFrom { get; set; }

    public Instant ValidTo { get; set; }

    public GridAreaCode GridAreaCode { get; set; } = null!;

    public ActorNumber GridAccessProvider { get; set; } = null!;

    public IReadOnlyCollection<ActorNumber> NeighborGridAreaOwners { get; set; } = [];

    public ConnectionState ConnectionState { get; set; }

    public MeteringPointType Type { get; set; }

    public MeteringPointSubType SubType { get; set; }

    public Resolution Resolution { get; set; } = null!;

    public MeasureUnit Unit { get; set; }

    public ProductId ProductId { get; set; }

    public MeteringPointIdentification? ParentIdentification { get; set; }

    public MeteringPointEnergySupplier EnergySupplier { get; set; } = null!;
}
