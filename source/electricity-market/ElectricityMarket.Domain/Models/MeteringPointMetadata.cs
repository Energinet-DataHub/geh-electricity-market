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

using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Domain.Models;

public sealed record MeteringPointMetadata(
    long Id,
    Interval Valid,
    MeteringPointIdentification? Parent,
    MeteringPointType Type,
    MeteringPointSubType SubType,
    ConnectionState ConnectionState,
    string Resolution,
    string GridAreaCode,
    string OwnedBy,
    ConnectionType? ConnectionType,
    DisconnectionType? DisconnectionType,
    Product Product,
    bool? ProductObligation,
    MeteringPointMeasureUnit MeasureUnit,
    AssetType? AssetType,
    bool? EnvironmentalFriendly,
    string? Capacity,
    decimal? PowerLimitKw,
    string? MeterNumber,
    int? NetSettlementGroup,
    int? ScheduledMeterReadingMonth,
    string? ExchangeFromGridAreaCode,
    string? ExchangeToGridAreaCode,
    string? PowerPlantGsrn,
    SettlementMethod? SettlementMethod,
    InstallationAddress InstallationAddress,
    string TransactionType);
