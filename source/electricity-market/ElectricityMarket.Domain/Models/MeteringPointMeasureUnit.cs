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
/// Metering point units.
/// </summary>
public enum MeteringPointMeasureUnit
{
    /// <summary>
    /// Code: AMP.
    /// </summary>
    Ampere,

    /// <summary>
    /// Code: H87.
    /// </summary>
    STK,

    /// <summary>
    /// Code: K3.
    /// </summary>
    VArh,

    /// <summary>
    /// Code: KWH.
    /// </summary>
    KWh,

    /// <summary>
    /// Code: KWT.
    /// </summary>
    KW,

    /// <summary>
    /// Code: MAW.
    /// </summary>
    MW,

    /// <summary>
    /// Code: MWH.
    /// </summary>
    MWh,

    /// <summary>
    /// Code: TNE.
    /// </summary>
    Tonne,

    /// <summary>
    /// Code: Z03.
    /// </summary>
    MVAr,

    /// <summary>
    /// Code: Z14.
    /// </summary>
    DanishTariffCode,
}
