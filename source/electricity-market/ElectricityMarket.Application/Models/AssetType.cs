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

namespace Energinet.DataHub.ElectricityMarket.Application.Models;

/// <summary>
/// AssetType type of a metering point.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Code: D01.
    /// </summary>
    SteamTurbineWithBackPressureMode,

    /// <summary>
    /// Code: D02.
    /// </summary>
    GasTurbine,

    /// <summary>
    /// Code: D03.
    /// </summary>
    CombinedCycle,

    /// <summary>
    /// Code: D04.
    /// </summary>
    CombustionEngineGas,

    /// <summary>
    /// Code: D05.
    /// </summary>
    SteamTurbineWithCondensationSteam,

    /// <summary>
    /// Code: D06.
    /// </summary>
    Boiler,

    /// <summary>
    /// Code: D07.
    /// </summary>
    StirlingEngine,

    /// <summary>
    /// Code: D08.
    /// </summary>
    PermanentConnectedElectricalEnergyStorageFacilities,

    /// <summary>
    /// Code: D09.
    /// </summary>
    TemporarilyConnectedElectricalEnergyStorageFacilities,

    /// <summary>
    /// Code: D10.
    /// </summary>
    FuelCells,

    /// <summary>
    /// Code: D11.
    /// </summary>
    PhotoVoltaicCells,

    /// <summary>
    /// Code: D12.
    /// </summary>
    WindTurbines,

    /// <summary>
    /// Code: D13.
    /// </summary>
    HydroelectricPower,

    /// <summary>
    /// Code: D14.
    /// </summary>
    WavePower,

    /// <summary>
    /// Code: D15.
    /// </summary>
    MixedProduction,

    /// <summary>
    /// Code: D16.
    /// </summary>
    ProductionWithElectricalEnergyStorageFacilities,

    /// <summary>
    /// Code: D17.
    /// </summary>
    PowerToX,

    /// <summary>
    /// Code: D18.
    /// </summary>
    RegenerativeDemandFacility,

    /// <summary>
    /// Code: D19.
    /// </summary>
    CombustionEngineDiesel,

    /// <summary>
    /// Code: D20.
    /// </summary>
    CombustionEngineBio,

    /// <summary>
    /// Code: D98.
    /// </summary>
    NoTechnology,

    /// <summary>
    /// Code: D99.
    /// </summary>
    UnknownTechnology,
}
