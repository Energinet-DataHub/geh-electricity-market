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

public static class ExternalMeteringPointTypeMapper
{
    public static string Map(string externalValue)
    {
        return externalValue switch
        {
            "D01" => "VEProduction",
            "D02" => "Analysis",
            "D03" => "NotUsed",
            "D04" => "SurplusProductionGroup6",
            "D05" => "NetProduction",
            "D06" => "SupplyToGrid",
            "D07" => "ConsumptionFromGrid",
            "D08" => "WholesaleServicesOrInformation",
            "D09" => "OwnProduction",
            "D10" => "NetFromGrid",
            "D11" => "NetToGrid",
            "D12" => "TotalConsumption",
            "D13" => "NetLossCorrection",
            "D14" => "ElectricalHeating",
            "D15" => "NetConsumption",
            "D17" => "OtherConsumption",
            "D18" => "OtherProduction",
            "D19" => "CapacitySettlement",
            "D20" => "ExchangeReactiveEnergy",
            "D21" => "CollectiveNetProduction",
            "D22" => "CollectiveNetConsumption",
            "D23" => "ActivatedDownregulation",
            "D24" => "ActivatedUpregulation",
            "D25" => "ActualConsumption",
            "D26" => "ActualProduction",
            "D99" => "InternalUse",
            "E17" => "Consumption",
            "E18" => "Production",
            "E20" => "Exchange",
            _ => $"Unmapped: {externalValue}",
        };
    }
}
