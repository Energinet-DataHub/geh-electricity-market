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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Entities;
using Energinet.DataHub.ElectricityMarket.Integration;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Extensions;

public static class MeteringPointPeriodEntityHelper
{
    public static MeteringPointPeriodEntity Create(
        Instant? validFrom = null,
        Instant? validTo = null,
        GridAreaCode? gridAreaCode = null,
        ActorNumber? gridAccessProvider = null,
        ConnectionState? connectionState = null,
        SubType? subType = null,
        Resolution? resolution = null,
        MeasureUnit? unit = null,
        ProductCode? productId = null)
    {
        return new MeteringPointPeriodEntity
        {
            ValidFrom = validFrom ?? SystemClock.Instance.GetCurrentInstant(),
            ValidTo = validTo ?? SystemClock.Instance.GetCurrentInstant(),
            GridAreaCode = gridAreaCode?.Value ?? "123",
            GridAccessProvider = gridAccessProvider?.Value ?? "9478450860603",
            ConnectionState = connectionState is not null ? (int)connectionState : 0,
            SubType = subType is not null ? (int)subType : 0,
            Resolution = resolution?.Value ?? "PT15M",
            Unit = unit is not null ? (int)unit : 0,
            ProductId = productId is not null ? (int)productId : 0,
        };
    }
}
