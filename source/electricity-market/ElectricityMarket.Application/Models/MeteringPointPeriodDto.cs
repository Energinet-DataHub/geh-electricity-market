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

namespace Energinet.DataHub.ElectricityMarket.Application.Models;

public sealed record MeteringPointPeriodDto(
    long Id,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    DateTimeOffset CreatedAt,
    string GridAreaCode,
    string OwnedBy,
    string ConnectionState,
    string Type,
    string SubType,
    string Resolution,
    string Unit,
    string ProductId,
    int ScheduledMeterReadingMonth,
    string AssetType,
    string DisconnectionType,
    string FuelType,
    string FromGridAreaCode,
    string ToGridAreaCode,
    string MeterNumber,
    string MeterReadingOccurrence,
    string Capacity,
    string ConnectionType,
    string NetSettlementGroup,
    MeteringPointPeriodDto? ParentMeteringPoint,
    long PowerLimitKw,
    string PowerPlantGsrn,
    string ProductCode,
    string ProductionObligation, // TODO: This is a string in databricks, but it looks like it should be a boolean if you look at the data, but i don't know what to map it to
    string ScheduledMeterReading,
    InstallationAddressDto InstallationAddress,
    string CalculationType,
    string SettlementMethod,
    DateTimeOffset EffectuationDate,
    string TransactionType);
