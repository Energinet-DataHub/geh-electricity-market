﻿// Copyright 2020 Energinet DataHub A/S
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

public static class CommercialRelationEntityHelper
{
    public static CommercialRelationEntity Create(
        ActorNumber? energySupplier = null,
        Instant? startDate = null,
        Instant? endDate = null)
    {
        return new CommercialRelationEntity
        {
            EnergySupplier = energySupplier?.Value ?? "4212712623156",
            StartDate = startDate ?? SystemClock.Instance.GetCurrentInstant(),
            EndDate = endDate ?? SystemClock.Instance.GetCurrentInstant(),
        };
    }
}
