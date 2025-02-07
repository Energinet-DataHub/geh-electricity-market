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

using Energinet.DataHub.ElectricityMarket.Domain.Models.Common;

namespace Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;

public sealed class Actor
{
    public Actor(
        ActorId id,
        OrganizationId organizationId,
        ActorNumber actorNumber,
        ActorMarketRole marketRole,
        ActorName name)
    {
        Id = id;
        OrganizationId = organizationId;
        ActorNumber = actorNumber;
        Name = name;
        MarketRole = marketRole;
    }

    /// <summary>
    /// The internal id of actor.
    /// </summary>
    public ActorId Id { get; }

    /// <summary>
    /// The id of the organization the actor belongs to.
    /// </summary>
    public OrganizationId OrganizationId { get; }

    /// <summary>
    /// The global location number of the current actor.
    /// </summary>
    public ActorNumber ActorNumber { get; }

    /// <summary>
    /// The Name of the current actor.
    /// </summary>
    public ActorName Name { get; set; }

    /// <summary>
    /// The role (function and permissions) assigned to the current actor.
    /// </summary>
    public ActorMarketRole MarketRole { get; private set; }
}
