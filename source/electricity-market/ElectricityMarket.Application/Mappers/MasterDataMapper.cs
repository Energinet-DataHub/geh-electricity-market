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
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using ConnectionState = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.ConnectionState;
using MeasureUnit = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeasureUnit;
using MeteringPointEnergySupplier = Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData.MeteringPointEnergySupplier;
using MeteringPointMasterData = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeteringPointMasterData;
using MeteringPointSubType = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeteringPointSubType;
using MeteringPointType = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.MeteringPointType;
using ProductId = Energinet.DataHub.ElectricityMarket.Domain.Models.MasterData.ProductId;

namespace Energinet.DataHub.ElectricityMarket.Application.Mappers;

internal sealed class MasterDataMapper
{
    public static Integration.Models.MasterData.MeteringPointMasterData Map(MeteringPointMasterData entity)
    {
        return new Integration.Models.MasterData.MeteringPointMasterData()
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

    private static MeteringPointEnergySupplier Map(MeteringPointRecipient entity)
    {
        return new MeteringPointEnergySupplier()
        {
            Identification = new MeteringPointIdentification(entity.Identification.Value),
            EnergySupplier = ActorNumber.Create(entity.ActorNumber.Value),
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static Integration.Models.MasterData.MeasureUnit Map(MeasureUnit inputUnit)
    {
        return inputUnit switch
        {
            MeasureUnit.MW => Integration.Models.MasterData.MeasureUnit.MW,
            MeasureUnit.MWh => Integration.Models.MasterData.MeasureUnit.MWh,
            MeasureUnit.Ampere => Integration.Models.MasterData.MeasureUnit.Ampere,
            MeasureUnit.kW => Integration.Models.MasterData.MeasureUnit.kW,
            MeasureUnit.kWh => Integration.Models.MasterData.MeasureUnit.kWh,
            MeasureUnit.Tonne => Integration.Models.MasterData.MeasureUnit.Tonne,
            MeasureUnit.kVArh => Integration.Models.MasterData.MeasureUnit.kVArh,
            MeasureUnit.DanishTariffCode => Integration.Models.MasterData.MeasureUnit.DanishTariffCode,
            MeasureUnit.MVAr => Integration.Models.MasterData.MeasureUnit.MVAr,
            MeasureUnit.STK => Integration.Models.MasterData.MeasureUnit.STK,
            _ => throw new ArgumentOutOfRangeException(nameof(inputUnit), inputUnit, null),
        };
    }

    private static Integration.Models.MasterData.MeteringPointSubType Map(MeteringPointSubType inputMeteringPointType)
    {
        return inputMeteringPointType switch
        {
            MeteringPointSubType.Calculated => Integration.Models.MasterData.MeteringPointSubType.Calculated,
            MeteringPointSubType.Physical => Integration.Models.MasterData.MeteringPointSubType.Physical,
            MeteringPointSubType.Virtual => Integration.Models.MasterData.MeteringPointSubType.Virtual,
            _ => throw new ArgumentOutOfRangeException(nameof(inputMeteringPointType), inputMeteringPointType, null),
        };
    }

    private static Integration.Models.MasterData.MeteringPointType Map(MeteringPointType inputMeteringPointType)
    {
        return inputMeteringPointType switch
        {
            MeteringPointType.Consumption => Integration.Models.MasterData.MeteringPointType.Consumption,
            MeteringPointType.Production => Integration.Models.MasterData.MeteringPointType.Production,
            MeteringPointType.Exchange => Integration.Models.MasterData.MeteringPointType.Exchange,
            _ => throw new ArgumentOutOfRangeException(nameof(inputMeteringPointType), inputMeteringPointType, null),
        };
    }

    private static Integration.Models.MasterData.ConnectionState Map(ConnectionState inputConnectionState)
    {
        return inputConnectionState switch
        {
            ConnectionState.NotUsed => Integration.Models.MasterData.ConnectionState.NotUsed,
            ConnectionState.ClosedDown => Integration.Models.MasterData.ConnectionState.ClosedDown,
            ConnectionState.New => Integration.Models.MasterData.ConnectionState.New,
            ConnectionState.Connected => Integration.Models.MasterData.ConnectionState.Connected,
            ConnectionState.Disconnected => Integration.Models.MasterData.ConnectionState.Disconnected,
            _ => throw new ArgumentOutOfRangeException(nameof(inputConnectionState), inputConnectionState, null),
        };
    }

    private static Integration.Models.MasterData.ProductId Map(ProductId inputProductId)
    {
        return inputProductId switch
        {
            ProductId.Tariff => Integration.Models.MasterData.ProductId.Tariff,
            ProductId.FuelQuantity => Integration.Models.MasterData.ProductId.FuelQuantity,
            ProductId.PowerActive => Integration.Models.MasterData.ProductId.Tariff,
            ProductId.PowerReactive => Integration.Models.MasterData.ProductId.PowerReactive,
            ProductId.EnergyActivate => Integration.Models.MasterData.ProductId.EnergyActivate,
            ProductId.EnergyReactive => Integration.Models.MasterData.ProductId.EnergyReactive,
            _ => throw new ArgumentOutOfRangeException(nameof(inputProductId), inputProductId, null),
        };
    }
}
