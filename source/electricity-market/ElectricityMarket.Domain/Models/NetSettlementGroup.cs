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

namespace Energinet.DataHub.ElectricityMarket.Domain.Models;

public sealed class NetSettlementGroup
{
    private NetSettlementGroup(int code, string description)
    {
        Code = code;
        Description = description;
    }

    public static NetSettlementGroup Group6 { get; } = new(6, "Yearly");

    public int Code { get; }

    public string Description { get; }
}
