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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public static class MeteringPointPeriodFactory
{
    public static MeteringPointPeriodEntity CreateMeteringPointPeriod(ImportedTransactionEntity importedTransaction)
    {
        ArgumentNullException.ThrowIfNull(importedTransaction);

        var meteringPointPeriod = new MeteringPointPeriodEntity
        {
            ValidFrom = importedTransaction.valid_from_date,
            ValidTo = importedTransaction.valid_to_date,
            CreatedAt = importedTransaction.dh2_created,
            ParentIdentification = importedTransaction.parent_metering_point_id?.TrimEnd(),
            Type = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.MeteringPointTypes, importedTransaction.type_of_mp),
            SubType = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.MeteringPointSubTypes, importedTransaction.sub_type_of_mp),
            ConnectionState = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.ConnectionStates, importedTransaction.physical_status_of_mp),
            Resolution = importedTransaction.meter_reading_occurrence.TrimEnd(),
            GridAreaCode = importedTransaction.metering_grid_area_id.TrimEnd(),
            ConnectionType = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.ConnectionTypes, importedTransaction.mp_connection_type),
            DisconnectionType = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.DisconnectionTypes, importedTransaction.disconnection_type),
            Product = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.Products, importedTransaction.product),
            ProductObligation = importedTransaction.product_obligation,
            MeasureUnit = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.MeasureUnits, importedTransaction.energy_timeseries_measure_unit),
            AssetType = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.AssetTypes, importedTransaction.asset_type),
            FuelType = importedTransaction.fuel_type,
            Capacity = importedTransaction.mp_capacity?.TrimEnd(),
            PowerLimitKw = importedTransaction.power_limit_kw,
            PowerLimitA = importedTransaction.power_limit_a,
            MeterNumber = importedTransaction.meter_number?.TrimEnd(),
            SettlementGroup = importedTransaction.net_settlement_group,
            ScheduledMeterReadingMonth = -1, // TODO: Requires custom logic.
            ExchangeFromGridArea = importedTransaction.from_grid_area?.TrimEnd(),
            ExchangeToGridArea = importedTransaction.to_grid_area?.TrimEnd(),
            PowerPlantGsrn = importedTransaction.power_plant_gsrn?.TrimEnd(),
            SettlementMethod = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.SettlementMethods, importedTransaction.settlement_method),
            InstallationAddress = new InstallationAddressEntity
            {
                StreetCode = importedTransaction.location_street_code?.TrimEnd(),
                StreetName = importedTransaction.location_street_name?.TrimEnd() ?? string.Empty,
                BuildingNumber = importedTransaction.location_building_number?.TrimEnd() ?? string.Empty,
                CityName = importedTransaction.location_city_name?.TrimEnd() ?? string.Empty,
                CitySubdivisionName = importedTransaction.location_city_subdivision_name?.TrimEnd(),
                DarReference = importedTransaction.location_dar_reference != null
                    ? Guid.Parse(importedTransaction.location_dar_reference)
                    : null,
                CountryCode = importedTransaction.location_country_name?.TrimEnd() ?? string.Empty,
                Floor = importedTransaction.location_floor_id?.TrimEnd(),
                Room = importedTransaction.location_room_id?.TrimEnd(),
                PostCode = importedTransaction.location_postcode?.TrimEnd() ?? string.Empty,
                MunicipalityCode = importedTransaction.location_municipality_code?.TrimEnd(),
                LocationDescription = importedTransaction.location_location_description?.TrimEnd()
            },
            OwnedBy = string.Empty, // Works as an override, will be resolved through mark-part.
            BusinessTransactionDosId = importedTransaction.btd_trans_doss_id,
            MeteringPointStateId = importedTransaction.metering_point_state_id,
            EffectuationDate = importedTransaction.effectuation_date,
            TransactionType = importedTransaction.transaction_type.TrimEnd(),
        };
        return meteringPointPeriod;
    }

    public static MeteringPointPeriodEntity CopyMeteringPointPeriod(MeteringPointPeriodEntity source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var copy = new MeteringPointPeriodEntity
        {
            ValidTo = source.ValidTo,
            ValidFrom = source.ValidFrom,
            CreatedAt = source.CreatedAt,
            ParentIdentification = source.ParentIdentification,
            Type = source.Type,
            SubType = source.SubType,
            ConnectionState = source.ConnectionState,
            Resolution = source.Resolution,
            GridAreaCode = source.GridAreaCode,
            ConnectionType = source.ConnectionType,
            DisconnectionType = source.DisconnectionType,
            Product = source.Product,
            ProductObligation = source.ProductObligation,
            MeasureUnit = source.MeasureUnit,
            AssetType = source.AssetType,
            FuelType = source.FuelType,
            Capacity = source.Capacity,
            PowerLimitKw = source.PowerLimitKw,
            PowerLimitA = source.PowerLimitA,
            MeterNumber = source.MeterNumber,
            SettlementGroup = source.SettlementGroup,
            ScheduledMeterReadingMonth = source.ScheduledMeterReadingMonth,
            ExchangeFromGridArea = source.ExchangeFromGridArea,
            ExchangeToGridArea = source.ExchangeToGridArea,
            PowerPlantGsrn = source.PowerPlantGsrn,
            SettlementMethod = source.SettlementMethod,
            InstallationAddress = new InstallationAddressEntity
            {
                StreetCode = source.InstallationAddress.StreetCode,
                StreetName = source.InstallationAddress.StreetName,
                BuildingNumber = source.InstallationAddress.BuildingNumber,
                CityName = source.InstallationAddress.CityName,
                CitySubdivisionName = source.InstallationAddress.CitySubdivisionName,
                DarReference = source.InstallationAddress.DarReference,
                CountryCode = source.InstallationAddress.CountryCode,
                Floor = source.InstallationAddress.Floor,
                Room = source.InstallationAddress.Room,
                PostCode = source.InstallationAddress.PostCode,
                MunicipalityCode = source.InstallationAddress.MunicipalityCode,
                LocationDescription = source.InstallationAddress.LocationDescription
            },
            OwnedBy = source.OwnedBy,
            BusinessTransactionDosId = source.BusinessTransactionDosId,
            MeteringPointStateId = source.MeteringPointStateId,
            EffectuationDate = source.EffectuationDate,
            TransactionType = source.TransactionType,
        };
        return copy;
    }
}
