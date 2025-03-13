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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Domain.Models;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;

internal static class MeteringPointEnumMapper
{
    public static EnumMap<MeteringPointType>[] MeteringPointTypes { get; } =
    [
        new(MeteringPointType.VEProduction, "VEProduction", "D01"),
        new(MeteringPointType.Analysis, "Analysis", "D02"),
        new(MeteringPointType.NotUsed, "NotUsed", "D03"),
        new(MeteringPointType.SurplusProductionGroup6, "SurplusProductionGroup6", "D04"),
        new(MeteringPointType.NetProduction, "NetProduction", "D05"),
        new(MeteringPointType.SupplyToGrid, "SupplyToGrid", "D06"),
        new(MeteringPointType.ConsumptionFromGrid, "ConsumptionFromGrid", "D07"),
        new(MeteringPointType.WholesaleServicesOrInformation, "WholesaleServicesOrInformation", "D08"),
        new(MeteringPointType.OwnProduction, "OwnProduction", "D09"),
        new(MeteringPointType.NetFromGrid, "NetFromGrid", "D10"),
        new(MeteringPointType.NetToGrid, "NetToGrid", "D11"),
        new(MeteringPointType.TotalConsumption, "TotalConsumption", "D12"),
        new(MeteringPointType.NetLossCorrection, "NetLossCorrection", "D13"),
        new(MeteringPointType.ElectricalHeating, "ElectricalHeating", "D14"),
        new(MeteringPointType.NetConsumption, "NetConsumption", "D15"),
        new(MeteringPointType.OtherConsumption, "OtherConsumption", "D17"),
        new(MeteringPointType.OtherProduction, "OtherProduction", "D18"),
        new(MeteringPointType.CapacitySettlement, "CapacitySettlement", "D19"),
        new(MeteringPointType.ExchangeReactiveEnergy, "ExchangeReactiveEnergy", "D20"),
        new(MeteringPointType.CollectiveNetProduction, "CollectiveNetProduction", "D21"),
        new(MeteringPointType.CollectiveNetConsumption, "CollectiveNetConsumption", "D22"),
        new(MeteringPointType.ActivatedDownregulation, "ActivatedDownregulation", "D23"),
        new(MeteringPointType.ActivatedUpregulation, "ActivatedUpregulation", "D24"),
        new(MeteringPointType.ActualConsumption, "ActualConsumption", "D25"),
        new(MeteringPointType.ActualProduction, "ActualProduction", "D26"),
        new(MeteringPointType.InternalUse, "InternalUse", "D99"),
        new(MeteringPointType.Consumption, "Consumption", "E17"),
        new(MeteringPointType.Production, "Production", "E18"),
        new(MeteringPointType.Exchange, "Exchange", "E20"),
    ];

    public static EnumMap<MeteringPointSubType>[] MeteringPointSubTypes { get; } =
    [
        new(MeteringPointSubType.Physical, "Physical", "D01"),
        new(MeteringPointSubType.Virtual, "Virtual", "D02"),
        new(MeteringPointSubType.Calculated, "Calculated", "D03"),
    ];

    public static EnumMap<ConnectionState>[] ConnectionStates { get; } =
    [
        new(ConnectionState.NotUsed, "NotUsed", "D01"),
        new(ConnectionState.ClosedDown, "ClosedDown", "D02"),
        new(ConnectionState.New, "New", "D03"),
        new(ConnectionState.Connected, "Connected", "E22"),
        new(ConnectionState.Disconnected, "Disconnected", "E23"),
    ];

    public static EnumMap<ConnectionType>[] ConnectionTypes { get; } =
    [
        new(ConnectionType.Direct, "Direct", "D01"),
        new(ConnectionType.Installation, "Installation", "D02"),
    ];

    public static EnumMap<DisconnectionType>[] DisconnectionTypes { get; } =
    [
        new(DisconnectionType.RemoteDisconnection, "RemoteDisconnection", "D01"),
        new(DisconnectionType.ManualDisconnection, "ManualDisconnection", "D02"),
    ];

    public static EnumMap<Product>[] Products { get; } =
    [
        new(Product.Tariff, "Tariff", "5790001330590"),
        new(Product.FuelQuantity, "FuelQuantity", "5790001330606"),
        new(Product.PowerActive, "PowerActive", "8716867000016"),
        new(Product.PowerReactive, "PowerReactive", "8716867000023"),
        new(Product.EnergyActive, "EnergyActive", "8716867000030"),
        new(Product.EnergyReactive, "EnergyReactive", "8716867000047"),
    ];

    public static EnumMap<MeteringPointMeasureUnit>[] MeasureUnits { get; } =
    [
        new(MeteringPointMeasureUnit.Ampere, "Ampere", "AMP"),
        new(MeteringPointMeasureUnit.STK, "STK", "H87"),
        new(MeteringPointMeasureUnit.VArh, "VArh", "K3"),
        new(MeteringPointMeasureUnit.KWh, "kWh", "KWH"),
        new(MeteringPointMeasureUnit.KW, "kW", "KWT"),
        new(MeteringPointMeasureUnit.MW, "MW", "MAW"),
        new(MeteringPointMeasureUnit.MWh, "MWh", "MWH"),
        new(MeteringPointMeasureUnit.Tonne, "Tonne", "TNE"),
        new(MeteringPointMeasureUnit.MVAr, "MVAr", "Z03"),
        new(MeteringPointMeasureUnit.DanishTariffCode, "DanishTariffCode", "Z14"),
    ];

    public static EnumMap<AssetType>[] AssetTypes { get; } =
    [
        new(AssetType.SteamTurbineWithBackPressureMode, "SteamTurbineWithBackPressureMode", "D01"),
        new(AssetType.GasTurbine, "GasTurbine", "D02"),
        new(AssetType.CombinedCycle, "CombinedCycle", "D03"),
        new(AssetType.CombustionEngineGas, "CombustionEngineGas", "D04"),
        new(AssetType.SteamTurbineWithCondensationSteam, "SteamTurbineWithCondensationSteam", "D05"),
        new(AssetType.Boiler, "Boiler", "D06"),
        new(AssetType.StirlingEngine, "StirlingEngine", "D07"),
        new(AssetType.PermanentConnectedElectricalEnergyStorageFacilities, "PermanentConnectedElectricalEnergyStorageFacilities", "D08"),
        new(AssetType.TemporarilyConnectedElectricalEnergyStorageFacilities, "TemporarilyConnectedElectricalEnergyStorageFacilities", "D09"),
        new(AssetType.FuelCells, "FuelCells", "D10"),
        new(AssetType.PhotoVoltaicCells, "PhotoVoltaicCells", "D11"),
        new(AssetType.WindTurbines, "WindTurbines", "D12"),
        new(AssetType.HydroelectricPower, "HydroelectricPower", "D13"),
        new(AssetType.WavePower, "WavePower", "D14"),
        new(AssetType.MixedProduction, "MixedProduction", "D15"),
        new(AssetType.ProductionWithElectricalEnergyStorageFacilities, "ProductionWithElectricalEnergyStorageFacilities", "D16"),
        new(AssetType.PowerToX, "PowerToX", "D17"),
        new(AssetType.RegenerativeDemandFacility, "RegenerativeDemandFacility", "D18"),
        new(AssetType.CombustionEngineDiesel, "CombustionEngineDiesel", "D19"),
        new(AssetType.CombustionEngineBio, "CombustionEngineBio", "D20"),
        new(AssetType.NoTechnology, "NoTechnology", "D98"),
        new(AssetType.UnknownTechnology, "UnknownTechnology", "D99"),
    ];

    public static EnumMap<SettlementMethod>[] SettlementMethods { get; } =
    [
        new(SettlementMethod.FlexSettled, "FlexSettled", "D01"),
        new(SettlementMethod.Profiled, "Profiled", "E01"),
        new(SettlementMethod.NonProfiled, "NonProfiled", "E02"),
    ];

    public static T MapEntity<T>(IEnumerable<EnumMap<T>> lookup, string entityValue)
        where T : struct, Enum
    {
        var result = lookup.FirstOrDefault(v => v.EntityValue == entityValue);
        if (result == null)
            throw new InvalidOperationException($"Could not find value {entityValue} in map {typeof(T)}.");

        return result.Enum;
    }

    public static T? MapOptionalEntity<T>(IEnumerable<EnumMap<T>> lookup, string? entityValue)
        where T : struct, Enum
    {
        if (entityValue == null)
            return null;

        var result = lookup.FirstOrDefault(v => v.EntityValue == entityValue);
        if (result == null)
            throw new InvalidOperationException($"Could not find value {entityValue} in map {typeof(T)}.");

        return result.Enum;
    }

    [return: NotNullIfNotNull(nameof(dh2Value))]
    public static string? MapDh2ToEntity<T>(IEnumerable<EnumMap<T>> lookup, string? dh2Value)
        where T : Enum
    {
        dh2Value = dh2Value?.TrimEnd();

        if (string.IsNullOrWhiteSpace(dh2Value))
            return null;

        var result = lookup.FirstOrDefault(v => v.Dh2Value == dh2Value);
        if (result == null)
            throw new InvalidOperationException($"Could not find value {dh2Value} in map {typeof(T)}.");

        return result.EntityValue;
    }

    public sealed record EnumMap<T>(T Enum, string EntityValue, string Dh2Value)
        where T : Enum;
}
