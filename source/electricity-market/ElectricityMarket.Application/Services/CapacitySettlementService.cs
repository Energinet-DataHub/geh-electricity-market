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

using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class CapacitySettlementService : ICapacitySettlementService
{
    private readonly Instant _capacitySettlementEnabledFrom = Instant.FromUtc(2024, 12, 31, 23, 0, 0);

    // """
    // Metering point periods for consumption metering points (parent) that have a coupled 'capacity_settlement' metering point (child).
    //
    // It represents the timeline of the consumption metering points. The first period (given by period_from_date/period_from_to)
    // of each metering point starts when the metering point first time enters 'connected' state - or 'disconnected' if that
    // occurs first. After that, new period starts when (and only when) a 'move-in' occurs, and the previous period is then
    // terminated at that same time.
    //
    // Exclude rows where the period of the parent
    // - does not have any overlap with the period of the child metering point.
    // - ends before 2024-12-31 23:00:00
    //
    // Formatting is according to ADR-144 with the following constraints:
    // - No column may use quoted values
    // - All date/time values must include seconds
    // """
    public IEnumerable<CapacitySettlementPeriodDto> GetCapacitySettlementPeriods(MeteringPointHierarchy meteringPointHierarchy)
    {
        ArgumentNullException.ThrowIfNull(meteringPointHierarchy);

        if (!meteringPointHierarchy.Parent.MetadataTimeline.Any(x => x.Type == MeteringPointType.Consumption && x.ConnectionState == ConnectionState.Connected))
        {
            yield break;
        }

        var capacitySettlementMeteringPoints = meteringPointHierarchy.ChildMeteringPoints
            .Where(mp => mp.MetadataTimeline.Any(p => p.Type == MeteringPointType.CapacitySettlement))
            .ToList();

        if (capacitySettlementMeteringPoints.Count == 0)
        {
            yield break;
        }

        foreach (var capacitySettlementMeteringPoint in capacitySettlementMeteringPoints)
        {
            foreach (var period in GetCapacitySettlementPeriods(meteringPointHierarchy, capacitySettlementMeteringPoint))
            {
                yield return period;
            }
        }
    }

    private static bool DoIntervalsOverlap(Interval interval1, Interval interval2)
    {
        return interval1.Start < interval2.End && interval2.Start < interval1.End;
    }

    private IEnumerable<CapacitySettlementPeriodDto> GetCapacitySettlementPeriods(MeteringPointHierarchy meteringPointHierarchy, MeteringPoint capacitySettlementMeteringPoint)
    {
        var capacitySettlementPeriods = capacitySettlementMeteringPoint.MetadataTimeline
            .Where(p => p.Type == MeteringPointType.CapacitySettlement)
            .SkipWhile(p => p.ConnectionState != ConnectionState.Connected)
            .TakeWhile(p => p.ConnectionState != ConnectionState.ClosedDown)
            .Where(p => p.Valid.End > _capacitySettlementEnabledFrom)
            .Select(p => p.Valid)
            .OrderBy(p => p.Start)
            .Combine((a, b) => a.End == b.Start, (a, b) => new Interval(a.Start, b.End))
            .ToList();

        // Process each capacity settlement period
        foreach (var capacitySettlementPeriod in capacitySettlementPeriods)
        {
            var overlappingCommercialRelations = meteringPointHierarchy.Parent.CommercialRelationTimeline
                .Where(x => x.Period.Start != x.Period.End)
                .Combine((a, b) => a.Period.End == b.Period.Start && ((a.ClientId is not null && b.ClientId is null) || a.ClientId == b.ClientId), (a, b) => a with { Period = new Interval(a.Period.Start, b.Period.End) })
                .Where(x => DoIntervalsOverlap(x.Period, capacitySettlementPeriod))
                .OrderBy(x => x.Period.Start)
                .ToList();

            if (overlappingCommercialRelations.Count == 0)
            {
                // If no commercial relations, then use parent connected interval
                var parentFirstConnectedPeriod = meteringPointHierarchy.Parent.MetadataTimeline.FirstOrDefault(x => x.ConnectionState == ConnectionState.Connected);
                if (parentFirstConnectedPeriod is not null)
                {
                    var parentClosedDownDate = meteringPointHierarchy.Parent.MetadataTimeline.FirstOrDefault(x => x.ConnectionState == ConnectionState.ClosedDown)?.Valid.Start.ToDateTimeOffset() ?? DateTimeOffset.MaxValue;

                    yield return new CapacitySettlementPeriodDto(
                        meteringPointHierarchy.Parent.Identification.Value,
                        parentFirstConnectedPeriod.Valid.Start.ToDateTimeOffset(),
                        parentClosedDownDate,
                        capacitySettlementMeteringPoint.Identification.Value,
                        Truncate(capacitySettlementPeriod.Start.ToDateTimeOffset()),
                        capacitySettlementPeriod.End.ToDateTimeOffset());
                }
            }
            else
            {
                // Return a period for each commercial relation including an optional period if parent was connected first
                var parentFirstConnectedPeriod = meteringPointHierarchy.Parent.MetadataTimeline.FirstOrDefault(x => x.ConnectionState == ConnectionState.Connected);

                for (var i = 0; i < overlappingCommercialRelations.Count; i++)
                {
                    var commercialRelation = overlappingCommercialRelations[i];
                    if (i == 0 && parentFirstConnectedPeriod is not null && parentFirstConnectedPeriod.Valid.Start < commercialRelation.Period.Start)
                    {
                        yield return new CapacitySettlementPeriodDto(
                            meteringPointHierarchy.Parent.Identification.Value,
                            parentFirstConnectedPeriod.Valid.Start.ToDateTimeOffset(),
                            commercialRelation.Period.End.ToDateTimeOffset(),
                            capacitySettlementMeteringPoint.Identification.Value,
                            Truncate(capacitySettlementPeriod.Start.ToDateTimeOffset()),
                            capacitySettlementPeriod.End.ToDateTimeOffset());
                    }
                    else if (i == overlappingCommercialRelations.Count - 1)
                    {
                        var parentClosedDownDate = meteringPointHierarchy.Parent.MetadataTimeline.FirstOrDefault(x => x.ConnectionState == ConnectionState.ClosedDown)?.Valid.Start.ToDateTimeOffset() ?? DateTimeOffset.MaxValue;

                        yield return new CapacitySettlementPeriodDto(
                            meteringPointHierarchy.Parent.Identification.Value,
                            commercialRelation.Period.Start.ToDateTimeOffset(),
                            parentClosedDownDate,
                            capacitySettlementMeteringPoint.Identification.Value,
                            Truncate(capacitySettlementPeriod.Start.ToDateTimeOffset()),
                            capacitySettlementPeriod.End.ToDateTimeOffset());
                    }
                    else
                    {
                        yield return new CapacitySettlementPeriodDto(
                            meteringPointHierarchy.Parent.Identification.Value,
                            commercialRelation.Period.Start.ToDateTimeOffset(),
                            commercialRelation.Period.End.ToDateTimeOffset(),
                            capacitySettlementMeteringPoint.Identification.Value,
                            Truncate(capacitySettlementPeriod.Start.ToDateTimeOffset()),
                            capacitySettlementPeriod.End.ToDateTimeOffset());
                    }
                }
            }
        }
    }

    private DateTimeOffset Truncate(DateTimeOffset dateTimeOffset)
    {
        var enabledFromDateTimeOffset = _capacitySettlementEnabledFrom.ToDateTimeOffset();
        if (dateTimeOffset < enabledFromDateTimeOffset)
        {
            return enabledFromDateTimeOffset;
        }

        return dateTimeOffset;
    }
}
