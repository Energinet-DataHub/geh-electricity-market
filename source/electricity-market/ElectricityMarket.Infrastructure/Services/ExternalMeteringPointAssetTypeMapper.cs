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
            "D01" => "Steam turbine with back-pressure mode",
            "D02" => "Gas turbine",
            "D03" => "Combined cycle",
            "D04" => "Combustion engine gas",
            "D05" => "Steam turbine with condensation / steam",
            "D06" => "Boiler",
            "D07" => "Stirling engine",
            "D08" => "Permanent connected electrical energy storage facilities ",
            "D09" => "Temporarily connected electrical energy storage facilities ",
            "D10" => "Fuel Cells",
            "D11" => "Photo voltaic cells",
            "D12" => "Wind turbines",
            "D13" => "Hydroelectric power",
            "D14" => "Wave power",
            "D15" => "Mixed production ",
            "D16" => "Production with electrical energy storage facilities ",
            "D17" => "Power-to-X ",
            "D18" => "Regenerative demand facility ",
            "D19" => "Combustion engine – diesel",
            "D20" => "Combustion engine - bio",
            "D98" => "No technology",
            "D99" => "Unknown technology",
            _ => $"Unmapped: {externalValue}",
        };
    }
}
