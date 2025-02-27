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

namespace Energinet.DataHub.ElectricityMarket.Domain.Models;

/// <summary>
/// Product of a metering point.
/// </summary>
public enum Product
{
    /// <summary>
    /// Code: 5790001330590.
    /// </summary>
    Tariff,

    /// <summary>
    /// Code: 5790001330606.
    /// </summary>
    FuelQuantity,

    /// <summary>
    /// Code: 8716867000016.
    /// </summary>
    PowerActive,

    /// <summary>
    /// Code: 8716867000023.
    /// </summary>
    PowerReactive,

    /// <summary>
    /// Code: 8716867000030.
    /// </summary>
    EnergyActive,

    /// <summary>
    /// Code: 8716867000047.
    /// </summary>
    EnergyReactive,
}
