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

using System.Security.Cryptography;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class HullerLogService() : IHullerLogService
{
    private static readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
    private static readonly MeteringPointType[] _irrelevantMeteringPointTypes = [MeteringPointType.InternalUse];
    private static readonly MeteringPointSubType[] _irrelevantMeteringPointSubtypes = [MeteringPointSubType.Calculated];

    public IReadOnlyList<HullerLogDto> GetHullerLog(MeteringPoint meteringPoint)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);

        var response = new List<HullerLogDto>();

        var latestMetadataTimeline = meteringPoint.Metadata;

        if (_relevantConnectionStates.Contains(latestMetadataTimeline.ConnectionState)
            && !_irrelevantMeteringPointTypes.Contains(latestMetadataTimeline.Type)
            && !_irrelevantMeteringPointSubtypes.Contains(latestMetadataTimeline.SubType))
        {
            response.Add(new HullerLogDto(
                MeteringPointId: meteringPoint.Identification.Value,
                GridAreaCode: latestMetadataTimeline.GridAreaCode,
                Resolution: latestMetadataTimeline.Resolution,
                PeriodFromDate: latestMetadataTimeline.Valid.Start.ToDateTimeOffset(),
                PeriodToDate: latestMetadataTimeline.Valid.End.ToDateTimeOffset()));
        }

        return response;
    }
}
