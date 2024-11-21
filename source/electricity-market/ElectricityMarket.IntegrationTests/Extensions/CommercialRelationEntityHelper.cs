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
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Extensions;

public static class MeteringPointPeriodEntityHelper
{
    public static MeteringPointPeriodEntity Create(
        Instant? validFrom = null,
        Instant? validTo = null,
        string? gridAreaCode = null,
        string? gridAccessProvider = null,
        int? connectionState = null,
        int? subType = null,
        string? resolution = null,
        int? unit = null,
        int? productId = null)
    {
        return new MeteringPointPeriodEntity
        {
            ValidFrom = validFrom ?? SystemClock.Instance.GetCurrentInstant(),
            ValidTo = validTo ?? SystemClock.Instance.GetCurrentInstant(),
            GridAreaCode = gridAreaCode ?? "123",
            GridAccessProvider = gridAccessProvider ?? "9478450860603",
            ConnectionState = connectionState ?? 1,
            SubType = subType ?? 1,
            Resolution = resolution ?? "PT15M",
            Unit = unit ?? 1,
            ProductId = productId ?? 12,
        };
    }
}
