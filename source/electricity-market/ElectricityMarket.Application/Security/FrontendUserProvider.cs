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

using System.Security.Claims;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;

namespace Energinet.DataHub.ElectricityMarket.Application.Security;

public sealed class FrontendUserProvider : IUserProvider<FrontendUser>
{
    private const string ActorNumberClaim = "actornumber";
    private const string MarketRolesClaim = "marketroles";

    public Task<FrontendUser?> ProvideUserAsync(
        Guid userId,
        Guid actorId,
        bool multiTenancy,
        IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();

        var marketRoleClaim = claimsList
            .Single(claim => claim.Type.Equals(MarketRolesClaim, StringComparison.OrdinalIgnoreCase)).Value;

        var actorNumberClaim = claimsList
            .Single(claim => claim.Type.Equals(ActorNumberClaim, StringComparison.OrdinalIgnoreCase)).Value;

        return Task.FromResult<FrontendUser?>(new FrontendUser(
            userId,
            actorId,
            actorNumberClaim,
            multiTenancy,
            Enum.Parse<EicFunction>(marketRoleClaim)));
    }
}
