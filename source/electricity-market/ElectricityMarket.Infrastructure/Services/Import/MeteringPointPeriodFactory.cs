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

        var newMpPeriod = new MeteringPointPeriodEntity
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
        return newMpPeriod;
    }

    public static MeteringPointPeriodEntity CopyMeteringPointPeriod(MeteringPointPeriodEntity overlappingPeriod)
    {
        ArgumentNullException.ThrowIfNull(overlappingPeriod);

        var closedOverlappingPeriod = new MeteringPointPeriodEntity
        {
            ValidTo = overlappingPeriod.ValidTo,
            ValidFrom = overlappingPeriod.ValidFrom,
            CreatedAt = overlappingPeriod.CreatedAt,
            ParentIdentification = overlappingPeriod.ParentIdentification,
            Type = overlappingPeriod.Type,
            SubType = overlappingPeriod.SubType,
            ConnectionState = overlappingPeriod.ConnectionState,
            Resolution = overlappingPeriod.Resolution,
            GridAreaCode = overlappingPeriod.GridAreaCode,
            ConnectionType = overlappingPeriod.ConnectionType,
            DisconnectionType = overlappingPeriod.DisconnectionType,
            Product = overlappingPeriod.Product,
            ProductObligation = overlappingPeriod.ProductObligation,
            MeasureUnit = overlappingPeriod.MeasureUnit,
            AssetType = overlappingPeriod.AssetType,
            FuelType = overlappingPeriod.FuelType,
            Capacity = overlappingPeriod.Capacity,
            PowerLimitKw = overlappingPeriod.PowerLimitKw,
            PowerLimitA = overlappingPeriod.PowerLimitA,
            MeterNumber = overlappingPeriod.MeterNumber,
            SettlementGroup = overlappingPeriod.SettlementGroup,
            ScheduledMeterReadingMonth = overlappingPeriod.ScheduledMeterReadingMonth,
            ExchangeFromGridArea = overlappingPeriod.ExchangeFromGridArea,
            ExchangeToGridArea = overlappingPeriod.ExchangeToGridArea,
            PowerPlantGsrn = overlappingPeriod.PowerPlantGsrn,
            SettlementMethod = overlappingPeriod.SettlementMethod,
            InstallationAddress = new InstallationAddressEntity
            {
                StreetCode = overlappingPeriod.InstallationAddress.StreetCode,
                StreetName = overlappingPeriod.InstallationAddress.StreetName,
                BuildingNumber = overlappingPeriod.InstallationAddress.BuildingNumber,
                CityName = overlappingPeriod.InstallationAddress.CityName,
                CitySubdivisionName = overlappingPeriod.InstallationAddress.CitySubdivisionName,
                DarReference = overlappingPeriod.InstallationAddress.DarReference,
                CountryCode = overlappingPeriod.InstallationAddress.CountryCode,
                Floor = overlappingPeriod.InstallationAddress.Floor,
                Room = overlappingPeriod.InstallationAddress.Room,
                PostCode = overlappingPeriod.InstallationAddress.PostCode,
                MunicipalityCode = overlappingPeriod.InstallationAddress.MunicipalityCode,
                LocationDescription = overlappingPeriod.InstallationAddress.LocationDescription
            },
            OwnedBy = overlappingPeriod.OwnedBy,
            BusinessTransactionDosId = overlappingPeriod.BusinessTransactionDosId,
            MeteringPointStateId = overlappingPeriod.MeteringPointStateId,
            EffectuationDate = overlappingPeriod.EffectuationDate,
            TransactionType = overlappingPeriod.TransactionType,
        };
        return closedOverlappingPeriod;
    }
}
