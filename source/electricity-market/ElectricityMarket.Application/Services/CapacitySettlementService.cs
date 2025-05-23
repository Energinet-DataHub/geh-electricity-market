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

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class CapacitySettlementService : ICapacitySettlementService
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ILogger<CapacitySettlementService> _logger;
    private readonly Instant _capacitySettlementEnabledFrom = Instant.FromUtc(2024, 12, 31, 23, 0, 0);

    public CapacitySettlementService(IMeteringPointRepository meteringPointRepository, ILogger<CapacitySettlementService> logger)
    {
        _meteringPointRepository = meteringPointRepository;
        _logger = logger;
    }

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
    public async IAsyncEnumerable<ICapacitySettlementResult> GetCapacitySettlementPeriodsAsync(
        MeteringPoint meteringPoint, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);

        var meteringPointHierarchy = await GetMeteringPointHierarchyAsync(meteringPoint, cancellationToken).ConfigureAwait(false);

        // No need to consider this metering point
        if (meteringPointHierarchy is null)
        {
            yield break;
        }

        var capacitySettlementMeteringPoint =
            meteringPointHierarchy.ChildMeteringPoints.SingleOrDefault(mp =>
                mp.MetadataTimeline.Any(p => p.Type == MeteringPointType.CapacitySettlement));

        if (capacitySettlementMeteringPoint is null)
        {
            yield return new CapacitySettlementEmptyDto(meteringPoint.Identification.Value);
            yield break;
        }

        var capacitySettlementPeriod = capacitySettlementMeteringPoint.MetadataTimeline.First(p => p.Type == MeteringPointType.CapacitySettlement);
        var consumptionPeriod = meteringPointHierarchy.Parent.MetadataTimeline.FirstOrDefault(m => m.Type == MeteringPointType.Consumption);

        if (consumptionPeriod is null)
        {
            yield return new CapacitySettlementEmptyDto(meteringPoint.Identification.Value);
            yield break;
        }

        if (capacitySettlementPeriod.Valid.End < _capacitySettlementEnabledFrom)
        {
            yield return new CapacitySettlementEmptyDto(meteringPoint.Identification.Value);
            yield break;
        }

        var commercialRelationsToExport = GetIntervalsToExport(meteringPointHierarchy.Parent, capacitySettlementPeriod);

        var periodsList = commercialRelationsToExport.Select(relation => new CapacitySettlementPeriodDto(
            meteringPointHierarchy.Parent.Identification.Value,
            relation.Start.ToDateTimeOffset(),
            relation.End.ToDateTimeOffset(),
            capacitySettlementMeteringPoint.Identification.Value,
            GetCreatedTimestamp(capacitySettlementMeteringPoint),
            GetClosedDownTimestamp(capacitySettlementMeteringPoint)));

        foreach (var period in periodsList)
        {
            yield return period;
        }
    }

    private static DateTimeOffset? GetClosedDownTimestamp(MeteringPoint capacitySettlementMeteringPoint)
    {
        return capacitySettlementMeteringPoint.MetadataTimeline.OrderBy(period => period.Valid.Start)
            .FirstOrDefault(period => period.ConnectionState == ConnectionState.ClosedDown)?.Valid.Start.ToDateTimeOffset();
    }

    private static bool DoIntervalsOverlap(Interval interval1, Interval interval2)
    {
        return interval1.Start < interval2.End && interval2.Start < interval1.End;
    }

    private static IEnumerable<Interval> GetConnectedInterval(MeteringPoint consumptionMeteringPoint)
    {
        var connectedTimestamp = consumptionMeteringPoint.MetadataTimeline.OrderBy(period => period.Valid.Start)
            .FirstOrDefault(period => period.ConnectionState == ConnectionState.Connected)?.Valid;

        if (connectedTimestamp is not null)
        {
            return [connectedTimestamp.Value];
        }

        return ImmutableList<Interval>.Empty;
    }

    private IEnumerable<Interval> GetIntervalsToExport(MeteringPoint consumptionMeteringPoint, MeteringPointMetadata capacitySettlementPeriod)
    {
        var commercialRelations = consumptionMeteringPoint.CommercialRelationTimeline.Where(relation => relation.Period.End > _capacitySettlementEnabledFrom && DoIntervalsOverlap(relation.Period, capacitySettlementPeriod.Valid)).OrderBy(relation => relation.Period.Start).ToList();

        if (commercialRelations.Count == 0)
        {
            return GetConnectedInterval(consumptionMeteringPoint);
        }

        return commercialRelations.Select(relation => relation.Period);
    }

    private DateTimeOffset GetCreatedTimestamp(MeteringPoint capacitySettlementMeteringPoint)
    {
        var createdTimestamp = capacitySettlementMeteringPoint.MetadataTimeline.OrderBy(period => period.Valid.Start)
            .FirstOrDefault(period => period.ConnectionState == ConnectionState.Connected)?.Valid.Start;

        createdTimestamp ??= capacitySettlementMeteringPoint.MetadataTimeline.First(p => p.Type == MeteringPointType.CapacitySettlement).Valid.Start;

        return Truncate(createdTimestamp.Value.ToDateTimeOffset());
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

    private async Task<MeteringPointHierarchy?> GetMeteringPointHierarchyAsync(MeteringPoint meteringPoint, CancellationToken cancellationToken)
    {
        // This metering point is a parent
        if (meteringPoint.MetadataTimeline.All(period => period.Parent is null))
        {
            // This is a consumption parent metering point, so we must process the hierarchy
            if (meteringPoint.MetadataTimeline.Any(period => period.Type == MeteringPointType.Consumption))
            {
                IEnumerable<MeteringPoint> children = new List<MeteringPoint>();
                if (await _meteringPointRepository.HasCapacitySettlementChildMeteringPointAsync(meteringPoint.Identification.Value))
                {
                    children = await _meteringPointRepository.GetChildMeteringPointsAsync(meteringPoint.Identification.Value).ConfigureAwait(false);
                }

                return new MeteringPointHierarchy(meteringPoint, children);
            }

            // This is a non-consumption parent metering point, so skip it
            return null;
        }

        // This metering point is not a parent, but has type capacity settlement, so we must process the hierarchy
        if (meteringPoint.MetadataTimeline.Any(period => period.Type == MeteringPointType.CapacitySettlement))
        {
            var parentIdentification = meteringPoint.MetadataTimeline.First(period => period.Parent is not null).Parent!;
            var parent = await _meteringPointRepository.GetAsync(parentIdentification).ConfigureAwait(false);

            if (parent is null)
            {
                _logger.LogWarning("Skipping orphan metering point {Id}", meteringPoint.Id);
                return null;
            }

            // This is a consumption parent metering point, so we must process the hierarchy
            if (parent.MetadataTimeline.Any(period => period.Type == MeteringPointType.Consumption))
            {
                var children = new List<MeteringPoint> { meteringPoint };
                return new MeteringPointHierarchy(parent, children);
            }

            // Parent was not a consumption metering point, so skip it
            return null;
        }

        // Not a parent and not a capacity settlement metering point, so skip it
        return null;
    }
}
