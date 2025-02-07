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

using System.Linq;
using ElectricityMarket.Domain.Models.Actors;
using ElectricityMarket.Domain.Models.Common;
using ElectricityMarket.Domain.Models.GridAreas;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model.MarketParticipant;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;

internal static class ActorMapper
{
    public static Actor MapFromEntity(ActorEntity from)
    {
        var marketRole =
            new ActorMarketRole(
                from.MarketRole.Function,
                from.MarketRole
                    .GridAreas
                    .Select(grid => new ActorGridArea(
                        new GridAreaId(grid.GridAreaId))).ToList(),
                from.MarketRole.Comment);

        var actorNumber = ActorNumber.Create(from.ActorNumber);
        var actorName = new ActorName(from.Name);

        return new Actor(
            new ActorId(from.Id),
            new OrganizationId(from.OrganizationId),
            actorNumber,
            marketRole,
            actorName);
    }
}
