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

using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;

namespace Energinet.DataHub.ElectricityMarket.Application.Mappers;

internal sealed class MeteringPointMapper
{
    public static MeteringPointDto Map(MeteringPoint entity)
    {
        return new MeteringPointDto(
            entity.Id,
            entity.Identification.Value,
            entity.MeteringPointPeriods.Select(Map),
            entity.CommercialRelations.Select(CommercialRelationMapper.Map));
    }

    private static MeteringPointPeriodDto Map(MeteringPointPeriod meteringPointPeriodEntity)
    {
        return new MeteringPointPeriodDto(
            meteringPointPeriodEntity.Id,
            meteringPointPeriodEntity.ValidFrom.ToDateTimeOffset(),
            meteringPointPeriodEntity.ValidTo.ToDateTimeOffset(),
            meteringPointPeriodEntity.CreatedAt.ToDateTimeOffset(),
            meteringPointPeriodEntity.GridAreaCode,
            meteringPointPeriodEntity.OwnedBy,
            MapConnectionState(meteringPointPeriodEntity.ConnectionState),
            MapMeteringPointType(meteringPointPeriodEntity.Type),
            MapMeteringPointSubType(meteringPointPeriodEntity.SubType),
            meteringPointPeriodEntity.Resolution,
            MapMeteringPointUnit(meteringPointPeriodEntity.Unit),
            meteringPointPeriodEntity.ProductId,
            meteringPointPeriodEntity.ScheduledMeterReadingMonth,
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            null,
            50,
            "TBD",
            "TBD",
            "TBD",
            "TBD",
            new InstallationAddressDto(
                1,
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD",
                "TBD"),
            "TBD",
            "TBD",
            meteringPointPeriodEntity.EffectuationDate.ToDateTimeOffset(),
            meteringPointPeriodEntity.TransactionType);
    }

    private static ConnectionState MapConnectionState(string connectionState) => connectionState switch
    {
        "NotUsed" => ConnectionState.NotUsed,
        "ClosedDown" => ConnectionState.ClosedDown,
        "New" => ConnectionState.New,
        "Connected" => ConnectionState.Connected,
        "Disconnected" => ConnectionState.Disconnected,
        _ => throw new ArgumentOutOfRangeException(nameof(connectionState), connectionState, null)
    };

    private static MeteringPointType MapMeteringPointType(string meteringPointType) => meteringPointType switch
    {
        "VEProduction" => MeteringPointType.VEProduction,
        "Analysis" => MeteringPointType.Analysis,
        "NotUsed" => MeteringPointType.NotUsed,
        "SurplusProductionGroup6" => MeteringPointType.SurplusProductionGroup6,
        "NetProduction" => MeteringPointType.NetProduction,
        "SupplyToGrid" => MeteringPointType.SupplyToGrid,
        "ConsumptionFromGrid" => MeteringPointType.ConsumptionFromGrid,
        "WholesaleServicesOrInformation" => MeteringPointType.WholesaleServicesOrInformation,
        "OwnProduction" => MeteringPointType.OwnProduction,
        "NetFromGrid" => MeteringPointType.NetFromGrid,
        "NetToGrid" => MeteringPointType.NetToGrid,
        "TotalConsumption" => MeteringPointType.TotalConsumption,
        "NetLossCorrection" => MeteringPointType.NetLossCorrection,
        "ElectricalHeating" => MeteringPointType.ElectricalHeating,
        "NetConsumption" => MeteringPointType.NetConsumption,
        "OtherConsumption" => MeteringPointType.OtherConsumption,
        "OtherProduction" => MeteringPointType.OtherProduction,
        "CapacitySettlement" => MeteringPointType.CapacitySettlement,
        "ExchangeReactiveEnergy" => MeteringPointType.ExchangeReactiveEnergy,
        "CollectiveNetProduction" => MeteringPointType.CollectiveNetProduction,
        "CollectiveNetConsumption" => MeteringPointType.CollectiveNetConsumption,
        "ActivatedDownregulation" => MeteringPointType.ActivatedDownregulation,
        "ActivatedUpregulation" => MeteringPointType.ActivatedUpregulation,
        "ActualConsumption" => MeteringPointType.ActualConsumption,
        "ActualProduction" => MeteringPointType.ActualProduction,
        "InternalUse" => MeteringPointType.InternalUse,
        "Consumption" => MeteringPointType.Consumption,
        "Production" => MeteringPointType.Production,
        "Exchange" => MeteringPointType.Exchange,
        _ => throw new ArgumentOutOfRangeException(nameof(meteringPointType), meteringPointType, null)
    };

    private static MeteringPointSubType MapMeteringPointSubType(string meteringPointSubType) => meteringPointSubType switch
    {
        "Physical" => MeteringPointSubType.Physical,
        "Virtual" => MeteringPointSubType.Virtual,
        "Calculated" => MeteringPointSubType.Calculated,
        _ => throw new ArgumentOutOfRangeException(nameof(meteringPointSubType), meteringPointSubType, null)
    };

    private static MeteringPointUnit MapMeteringPointUnit(string meteringPointUnit) => meteringPointUnit switch
    {
        "Ampere" => MeteringPointUnit.Ampere,
        "STK" => MeteringPointUnit.STK,
        "VArh" => MeteringPointUnit.VArh,
        "kWh" => MeteringPointUnit.KWh,
        "kW" => MeteringPointUnit.KW,
        "MW" => MeteringPointUnit.MW,
        "MWh" => MeteringPointUnit.MWh,
        "Tonne" => MeteringPointUnit.Tonne,
        "MVAr" => MeteringPointUnit.MVAr,
        "DanishTariffCode" => MeteringPointUnit.DanishTariffCode,
        _ => throw new ArgumentOutOfRangeException(nameof(meteringPointUnit), meteringPointUnit, null)
    };
}
