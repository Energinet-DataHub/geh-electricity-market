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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public static class ExternalMeteringPointAssetTypeMapper
{
    public static string Map(string externalValue)
    {
        return externalValue switch
        {
            "D01" => "SteamTurbineWithBackPressureMode",
            "D02" => "GasTurbine",
            "D03" => "CombinedCycle",
            "D04" => "CombustionEngineGas",
            "D05" => "SteamTurbineWithCondensationSteam",
            "D06" => "Boiler",
            "D07" => "StirlingEngine",
            "D08" => "PermanentConnectedElectricalEnergyStorageFacilities",
            "D09" => "TemporarilyConnectedElectricalEnergyStorageFacilities",
            "D10" => "FuelCells",
            "D11" => "PhotoVoltaicCells",
            "D12" => "WindTurbines",
            "D13" => "HydroelectricPower",
            "D14" => "WavePower",
            "D15" => "MixedProduction",
            "D16" => "ProductionWithElectricalEnergyStorageFacilities",
            "D17" => "PowerToX",
            "D18" => "RegenerativeDemandFacility",
            "D19" => "CombustionEngineDiesel",
            "D20" => "CombustionEngineBio",
            "D98" => "NoTechnology",
            "D99" => "UnknownTechnology",
            _ => $"Unmapped: {externalValue}",
        };
    }
}
