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

using Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData;
using Energinet.DataHub.ElectricityMarket.Integration.Models;
using ConnectionState = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.ConnectionState;
using MeasureUnit = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeasureUnit;
using MeteringPointMasterData = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeteringPointMasterData;
using MeteringPointSubType = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeteringPointSubType;
using MeteringPointType = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeteringPointType;
using ProductId = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.ProductId;

namespace Energinet.DataHub.ElectricityMarket.Application.Mappers;

internal sealed class MasterDataMapper
{
    public static Integration.Models.MeteringPointMasterData Map(MeteringPointMasterData entity)
    {
        return new Integration.Models.MeteringPointMasterData()
        {
            Identification = new MeteringPointIdentification(entity.Identification.Value),
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            GridAreaCode = new GridAreaCode(entity.GridAreaCode.Value),
            GridAccessProvider = ActorNumber.Create(entity.GridAccessProvider.Value),
            NeighborGridAreaOwners = entity.NeighborGridAreaOwners.Select(x => ActorNumber.Create(x.Value)).ToList(),
            ConnectionState = Map(entity.ConnectionState),
            Type = Map(entity.Type),
            SubType = Map(entity.SubType),
            Resolution = new Resolution(entity.Resolution.Value),
            Unit = Map(entity.Unit),
            ProductId = Map(entity.ProductId),
            ParentIdentification = entity.ParentIdentification?.Value is null ? null : new MeteringPointIdentification(entity.ParentIdentification.Value),
            EnergySuppliers = entity.Recipients.Select(Map).ToList()
        };
    }

    private static Integration.Models.MeteringPointEnergySupplier Map(MeteringPointRecipient entity)
    {
        return new Integration.Models.MeteringPointEnergySupplier()
        {
            Identification = new MeteringPointIdentification(entity.Identification.Value),
            EnergySupplier = ActorNumber.Create(entity.ActorNumber.Value),
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static Integration.Models.MeasureUnit Map(MeasureUnit inputUnit)
    {
        return inputUnit switch
        {
            MeasureUnit.MW => Integration.Models.MeasureUnit.MW,
            MeasureUnit.MWh => Integration.Models.MeasureUnit.MWh,
            MeasureUnit.Ampere => Integration.Models.MeasureUnit.Ampere,
            MeasureUnit.kW => Integration.Models.MeasureUnit.kW,
            MeasureUnit.kWh => Integration.Models.MeasureUnit.kWh,
            MeasureUnit.Tonne => Integration.Models.MeasureUnit.Tonne,
            MeasureUnit.kVArh => Integration.Models.MeasureUnit.kVArh,
            MeasureUnit.DanishTariffCode => Integration.Models.MeasureUnit.DanishTariffCode,
            MeasureUnit.MVAr => Integration.Models.MeasureUnit.MVAr,
            MeasureUnit.STK => Integration.Models.MeasureUnit.STK,
            _ => throw new ArgumentOutOfRangeException(nameof(inputUnit), inputUnit, null),
        };
    }

    private static Integration.Models.MeteringPointSubType Map(MeteringPointSubType inputMeteringPointType)
    {
        return inputMeteringPointType switch
        {
            MeteringPointSubType.Calculated => Integration.Models.MeteringPointSubType.Calculated,
            MeteringPointSubType.Physical => Integration.Models.MeteringPointSubType.Physical,
            MeteringPointSubType.Virtual => Integration.Models.MeteringPointSubType.Virtual,
            _ => throw new ArgumentOutOfRangeException(nameof(inputMeteringPointType), inputMeteringPointType, null),
        };
    }

    private static Integration.Models.MeteringPointType Map(MeteringPointType inputMeteringPointType)
    {
        return inputMeteringPointType switch
        {
            MeteringPointType.Consumption => Integration.Models.MeteringPointType.Consumption,
            MeteringPointType.Production => Integration.Models.MeteringPointType.Production,
            MeteringPointType.Exchange => Integration.Models.MeteringPointType.Exchange,
            _ => throw new ArgumentOutOfRangeException(nameof(inputMeteringPointType), inputMeteringPointType, null),
        };
    }

    private static Integration.Models.ConnectionState Map(ConnectionState inputConnectionState)
    {
        return inputConnectionState switch
        {
            ConnectionState.NotUsed => Integration.Models.ConnectionState.NotUsed,
            ConnectionState.ClosedDown => Integration.Models.ConnectionState.ClosedDown,
            ConnectionState.New => Integration.Models.ConnectionState.New,
            ConnectionState.Connected => Integration.Models.ConnectionState.Connected,
            ConnectionState.Disconnected => Integration.Models.ConnectionState.Disconnected,
            _ => throw new ArgumentOutOfRangeException(nameof(inputConnectionState), inputConnectionState, null),
        };
    }

    private static Integration.Models.ProductId Map(ProductId inputProductId)
    {
        return inputProductId switch
        {
            ProductId.Tariff => Integration.Models.ProductId.Tariff,
            ProductId.FuelQuantity => Integration.Models.ProductId.FuelQuantity,
            ProductId.PowerActive => Integration.Models.ProductId.Tariff,
            ProductId.PowerReactive => Integration.Models.ProductId.PowerReactive,
            ProductId.EnergyActivate => Integration.Models.ProductId.EnergyActivate,
            ProductId.EnergyReactive => Integration.Models.ProductId.EnergyReactive,
            _ => throw new ArgumentOutOfRangeException(nameof(inputProductId), inputProductId, null),
        };
    }

    private static EicFunction Map(Domain.Models.Actors.EicFunction inputFunction)
    {
        return inputFunction switch
        {
            Domain.Models.Actors.EicFunction.GridAccessProvider => EicFunction.GridAccessProvider,
            Domain.Models.Actors.EicFunction.BalanceResponsibleParty => EicFunction.BalanceResponsibleParty,
            Domain.Models.Actors.EicFunction.BillingAgent => EicFunction.BillingAgent,
            Domain.Models.Actors.EicFunction.EnergySupplier => EicFunction.EnergySupplier,
            Domain.Models.Actors.EicFunction.ImbalanceSettlementResponsible => EicFunction.ImbalanceSettlementResponsible,
            Domain.Models.Actors.EicFunction.MeterOperator => EicFunction.MeterOperator,
            Domain.Models.Actors.EicFunction.MeteredDataAdministrator => EicFunction.MeteredDataAdministrator,
            Domain.Models.Actors.EicFunction.MeteredDataResponsible => EicFunction.MeteredDataResponsible,
            Domain.Models.Actors.EicFunction.MeteringPointAdministrator => EicFunction.MeteringPointAdministrator,
            Domain.Models.Actors.EicFunction.SystemOperator => EicFunction.SystemOperator,
            Domain.Models.Actors.EicFunction.DanishEnergyAgency => EicFunction.DanishEnergyAgency,
            Domain.Models.Actors.EicFunction.DataHubAdministrator => EicFunction.DataHubAdministrator,
            Domain.Models.Actors.EicFunction.IndependentAggregator => EicFunction.IndependentAggregator,
            Domain.Models.Actors.EicFunction.SerialEnergyTrader => EicFunction.SerialEnergyTrader,
            Domain.Models.Actors.EicFunction.Delegated => EicFunction.Delegated,
            Domain.Models.Actors.EicFunction.ItSupplier => EicFunction.ItSupplier,
            _ => throw new ArgumentOutOfRangeException(nameof(inputFunction), inputFunction, null),
        };
    }
}
