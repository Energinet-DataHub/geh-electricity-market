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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public static class ExternalMeteringPointProductIdMapper
{
    public static string Map(string externalValue)
    {
        return externalValue switch
        {
            "5790001330590" => "Tariff",
            "5790001330606" => "Fuel quantity",
            "8716867000016" => "Power active",
            "8716867000023" => "Power reactive",
            "8716867000030" => "Energy active",
            "8716867000047" => "Energy reactive",
            _ => $"Unmapped: {externalValue}",
        };
    }
}
