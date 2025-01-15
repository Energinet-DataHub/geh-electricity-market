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

namespace Energinet.DataHub.ElectricityMarket.Integration;

public sealed class MeteringPointMasterData
{
    public MeteringPointIdentification Identification { get; internal set; } = null!;

    public GridAreaCode GridAreaCode { get; internal set; } = null!;

    public ActorNumber GridAccessProvider { get; internal set; } = null!;

    public ActorNumber? NeighborGridAreaOwner { get; internal set; }

    public ConnectionState ConnectionState { get; internal set; }

    public MeteringPointType Type { get; internal set; }

    public MeteringPointSubType SubType { get; internal set; }

    public Resolution Resolution { get; internal set; } = null!;

    public MeasureUnit Unit { get; internal set; }

    public ProductId ProductId { get; internal set; }

    public MeteringPointIdentification? ParentIdentification { get; internal set; }
}
