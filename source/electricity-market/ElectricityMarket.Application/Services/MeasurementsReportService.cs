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

using Energinet.DataHub.ElectricityMarket.Application.Helpers;
using Energinet.DataHub.ElectricityMarket.Application.Helpers.Timeline;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class MeasurementsReportService() : IMeasurementsReportService
{
    private static readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
    private static readonly Instant _cutOffDate = SystemClock.Instance.GetCurrentInstant().InZone(DateTimeZone.Utc).LocalDateTime.PlusYears(-8).InZoneStrictly(DateTimeZone.Utc).ToInstant();

    private readonly SnakeCaseFormatter _snakeCaseFormatter = new();

    public IEnumerable<MeasurementsReportDto> GetMeasurementsReport(MeteringPointHierarchy meteringPointHierarchy)
    {
        ArgumentNullException.ThrowIfNull(meteringPointHierarchy);

        var segments = BuildMergedTimeline(meteringPointHierarchy);

        foreach (var segment in segments.Where(s =>
            _relevantConnectionStates.Contains(s.Metadata.ConnectionState)
            && s.Period.End > _cutOffDate))
        {
            var energySupplierId = segment.Relation?.EnergySupplier;

            yield return new MeasurementsReportDto(
                MeteringPointId: segment.Identification!.Value,
                MeteringPointType: _snakeCaseFormatter.ToSnakeCase(segment.Metadata.Type.ToString()),
                GridAreaCode: segment.Metadata.GridAreaCode,
                Resolution: segment.Metadata.Resolution,
                EnergySupplierId: energySupplierId,
                PhysicalStatus: _snakeCaseFormatter.ToSnakeCase(segment.Metadata.ConnectionState.ToString()),
                QuantityUnit: segment.Metadata.MeasureUnit.ToString(),
                FromGridAreaCode: segment.Metadata.ExchangeFromGridAreaCode,
                ToGridAreaCode: segment.Metadata.ExchangeToGridAreaCode,
                PeriodFromDate: segment.Period.Start.ToDateTimeOffset(),
                PeriodToDate: segment.Period.End == Instant.MaxValue ? null : segment.Period.End.ToDateTimeOffset());
        }
    }

    private static List<TimelineSegment> BuildMergedTimeline(
    MeteringPointHierarchy hierarchy)
    {
        var allSegments = new List<TimelineSegment>();

        foreach (var mp in new[] { hierarchy.Parent }
                     .Concat(hierarchy.ChildMeteringPoints))
        {
            var changePoints = new SortedSet<Instant>();

            foreach (var mt in mp.MetadataTimeline)
            {
                changePoints.Add(mt.Valid.Start);
                if (mt.Valid.End != Instant.MaxValue)
                    changePoints.Add(mt.Valid.End);
            }

            foreach (var cr in mp.CommercialRelationTimeline)
            {
                changePoints.Add(cr.Period.Start);
                if (cr.Period.End != Instant.MaxValue)
                    changePoints.Add(cr.Period.End);
            }

            var builder = new TimelineBuilder();
            foreach (var start in changePoints)
            {
                var metadata = mp.MetadataTimeline
                    .First(m => m.Valid.Contains(start));
                var commercialRelation = mp.CommercialRelationTimeline
                    .FirstOrDefault(r => r.Period.Contains(start));

                builder.AddSegment(
                    mp.Identification,
                    start,
                    metadata,
                    commercialRelation);
            }

            allSegments.AddRange(builder.Build());
        }

        return allSegments;
    }
}
