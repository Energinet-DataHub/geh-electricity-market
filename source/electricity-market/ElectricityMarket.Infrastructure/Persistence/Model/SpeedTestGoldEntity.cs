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

public sealed class SpeedTestGoldEntity
{
    public int Id { get; set; }

    public string MeteringPointId { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset ValidTo { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public string GridArea { get; set; }

    public long StateId { get; set; }

    public long TransDossId { get; set; }
}
