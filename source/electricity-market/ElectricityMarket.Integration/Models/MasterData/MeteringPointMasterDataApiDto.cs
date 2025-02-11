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

namespace Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;

public sealed class MeteringPointMasterDataDto
{
    public MeteringPointMasterDataDto(
        string identification,
        DateTimeOffset validFrom,
        DateTimeOffset validTo,
        string gridAreaCode,
        string gridAccessProvider,
        IReadOnlyCollection<string> neighborGridAreaOwners,
        ConnectionState connectionState,
        MeteringPointType type,
        MeteringPointSubType subType,
        string resolution,
        MeasureUnit unit,
        ProductId productId,
        string? parentIdentification,
        IReadOnlyCollection<MeteringPointRecipientDto> recipients)
    {
        Identification = identification;
        ValidFrom = validFrom;
        ValidTo = validTo;
        GridAreaCode = gridAreaCode;
        GridAccessProvider = gridAccessProvider;
        NeighborGridAreaOwners = neighborGridAreaOwners;
        ConnectionState = connectionState;
        Type = type;
        SubType = subType;
        Resolution = resolution;
        Unit = unit;
        ProductId = productId;
        ParentIdentification = parentIdentification;
        Recipients = recipients;
    }

    public string Identification { get; init; }
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset ValidTo { get; init; }
    public string GridAreaCode { get; init; }
    public string GridAccessProvider { get; init; }
    public IReadOnlyCollection<string> NeighborGridAreaOwners { get; init; }
    public ConnectionState ConnectionState { get; init; }
    public MeteringPointType Type { get; init; }
    public MeteringPointSubType SubType { get; init; }
    public string Resolution { get; init; }
    public MeasureUnit Unit { get; init; }
    public ProductId ProductId { get; init; }
    public string? ParentIdentification { get; init; }
    public IReadOnlyCollection<MeteringPointRecipientDto> Recipients { get; init; }

    public MeteringPointRecipientDto? CurrentRecipient =>
        Recipients.FirstOrDefault(x => x.StartDate <= DateTimeOffset.Now && x.EndDate >= DateTimeOffset.Now) ?? Recipients.OrderByDescending(x => x.StartDate).FirstOrDefault();
}
