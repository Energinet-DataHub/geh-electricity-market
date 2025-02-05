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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Models;

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
    MeteringPointPeriodDto? ParentMeteringPoint, // TODO: Check if this is correct
    long PowerLimitA,
    string PowerPlantGsrn,
    string ProductCode,
    string ProductionObligation,
    string ScheduledMeterReading,
    InstallationAddressDto InstallationAddress,
    string CalculationType,
    string SettlementMethod);
