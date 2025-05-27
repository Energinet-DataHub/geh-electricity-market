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

using Energinet.DataHub.ElectricityMarket.Application.Helpers.Timeline;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class HullerLogService() : IHullerLogService
{
    private static readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
    private static readonly MeteringPointType[] _irrelevantMeteringPointTypes = [MeteringPointType.InternalUse];
    private static readonly MeteringPointSubType[] _irrelevantMeteringPointSubtypes = [MeteringPointSubType.Calculated];
    private static readonly Instant _cutOffDate = SystemClock.Instance.GetCurrentInstant().InZone(DateTimeZone.Utc).LocalDateTime.PlusYears(-3).InZoneStrictly(DateTimeZone.Utc).ToInstant();

    public IReadOnlyList<HullerLogDto> GetHullerLog(MeteringPoint meteringPoint)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);

        var segments = BuildMergedTimeline(meteringPoint);

        var response = new List<HullerLogDto>();

        foreach (var segment in segments.Where(s =>
            _relevantConnectionStates.Contains(s.Metadata.ConnectionState)
            && !_irrelevantMeteringPointTypes.Contains(s.Metadata.Type)
            && !_irrelevantMeteringPointSubtypes.Contains(s.Metadata.SubType)
            && (s.Metadata.Valid.Start >= _cutOffDate || (s.Metadata.Valid.Start < _cutOffDate && s.Metadata.Valid.End > _cutOffDate))))
        {
            response.Add(new HullerLogDto(
                MeteringPointId: meteringPoint.Identification.Value,
                GridAreaCode: segment.Metadata.GridAreaCode,
                Resolution: segment.Metadata.Resolution,
                PeriodFromDate: segment.Metadata.Valid.Start.ToDateTimeOffset(),
                PeriodToDate: segment.Metadata.Valid.End == Instant.MaxValue ? null : segment.Metadata.Valid.End.ToDateTimeOffset()));
        }

        return response;
    }

    private static IReadOnlyList<TimelineSegment> BuildMergedTimeline(MeteringPoint meteringPoint)
    {
        var changePoints = new SortedSet<Instant>();

        foreach (var m in meteringPoint.MetadataTimeline)
        {
            changePoints.Add(m.Valid.Start);
            if (m.Valid.End.ToDateTimeOffset() != DateTimeOffset.MaxValue)
            {
                changePoints.Add(m.Valid.End);
            }
        }

        var builder = new TimelineBuilder();
        foreach (var start in changePoints)
        {
            var metadata = meteringPoint.MetadataTimeline
                .First(m => m.Valid.Contains(start));

            builder.AddSegment(start, metadata, null);
        }

        return builder.Build();
    }
}
