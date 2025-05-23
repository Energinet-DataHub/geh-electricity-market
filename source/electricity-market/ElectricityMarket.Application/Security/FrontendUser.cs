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

using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;

namespace Energinet.DataHub.ElectricityMarket.Application.Security;

public sealed class FrontendUser
{
    public FrontendUser(Guid userId, Guid actorId, string actorNumber, bool isFas, EicFunction marketRole)
    {
        UserId = userId;
        ActorId = actorId;
        ActorNumber = actorNumber;
        IsFas = isFas;
        MarketRole = marketRole;
    }

    public Guid UserId { get; }
    public Guid ActorId { get; }
    public bool IsFas { get; }
    public string ActorNumber { get; }
    public EicFunction MarketRole { get; }

    public bool IsFasOrAssignedToActor(Guid actorId)
    {
        return IsFas || actorId == ActorId;
    }

    public bool IsAssignedToActor(Guid actorId)
    {
        return actorId == ActorId;
    }
}
