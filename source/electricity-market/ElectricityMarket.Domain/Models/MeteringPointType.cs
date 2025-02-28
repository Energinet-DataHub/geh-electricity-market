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
/// Types of metering point.
/// </summary>
public enum MeteringPointType
{
    /// <summary>
    /// Code: D01.
    /// </summary>
    VEProduction,

    /// <summary>
    /// Code: D02.
    /// </summary>
    Analysis,

    /// <summary>
    /// Code: D03.
    /// </summary>
    NotUsed,

    /// <summary>
    /// Code: D04.
    /// </summary>
    SurplusProductionGroup6,

    /// <summary>
    /// Code: D05.
    /// </summary>
    NetProduction,

    /// <summary>
    /// Code: D06.
    /// </summary>
    SupplyToGrid,

    /// <summary>
    /// Code: D07.
    /// </summary>
    ConsumptionFromGrid,

    /// <summary>
    /// Code: D08.
    /// </summary>
    WholesaleServicesOrInformation,

    /// <summary>
    /// Code: D09.
    /// </summary>
    OwnProduction,

    /// <summary>
    /// Code: D10.
    /// </summary>
    NetFromGrid,

    /// <summary>
    /// Code: D11.
    /// </summary>
    NetToGrid,

    /// <summary>
    /// Code: D12.
    /// </summary>
    TotalConsumption,

    /// <summary>
    /// Code: D13.
    /// </summary>
    NetLossCorrection,

    /// <summary>
    /// Code: D14.
    /// </summary>
    ElectricalHeating,

    /// <summary>
    /// Code: D15.
    /// </summary>
    NetConsumption,

    /// <summary>
    /// Code: D17.
    /// </summary>
    OtherConsumption,

    /// <summary>
    /// Code: D18.
    /// </summary>
    OtherProduction,

    /// <summary>
    /// Code: D19.
    /// </summary>
    CapacitySettlement,

    /// <summary>
    /// Code: D20.
    /// </summary>
    ExchangeReactiveEnergy,

    /// <summary>
    /// Code: D21.
    /// </summary>
    CollectiveNetProduction,

    /// <summary>
    /// Code: D22.
    /// </summary>
    CollectiveNetConsumption,

    /// <summary>
    /// Code: D23.
    /// </summary>
    ActivatedDownregulation,

    /// <summary>
    /// Code: D24.
    /// </summary>
    ActivatedUpregulation,

    /// <summary>
    /// Code: D25.
    /// </summary>
    ActualConsumption,

    /// <summary>
    /// Code: D26.
    /// </summary>
    ActualProduction,

    /// <summary>
    /// Code: D99.
    /// </summary>
    InternalUse,

    /// <summary>
    /// Code: E17.
    /// </summary>
    Consumption,

    /// <summary>
    /// Code: E18.
    /// </summary>
    Production,

    /// <summary>
    /// Code: E20.
    /// </summary>
    Exchange,
}
