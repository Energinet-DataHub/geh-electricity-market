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
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Common;
using Energinet.DataHub.ElectricityMarket.Domain.Models.GridAreas;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Common;

internal static class TestPreparationModels
{
    public static Actor MockedActor() => MockedActor(Guid.NewGuid(), Guid.NewGuid());

    public static Actor MockedActor(Guid actorId) => MockedActor(actorId, Guid.NewGuid());

    public static Actor MockedActor(Guid actorId, Guid organizationId) => new(
        new ActorId(actorId),
        new OrganizationId(organizationId),
        new MockedGln(),
        new ActorMarketRole(EicFunction.GridAccessProvider, [], null),
        new ActorName("Racoon Power"));

    public static Actor MockedActiveActor(Guid actorId, Guid organizationId) => new(
        new ActorId(actorId),
        new OrganizationId(organizationId),
        new MockedGln(),
        new ActorMarketRole(EicFunction.GridAccessProvider, [], null),
        new ActorName("Racoon Power"));

    public static GridArea MockedGridArea() => new(
        new GridAreaId(Guid.NewGuid()),
        new GridAreaName("Raccoon City"),
        new GridAreaCode("420"),
        PriceAreaCode.Dk1,
        GridAreaType.Distribution,
        DateTimeOffset.MinValue,
        null);
}
