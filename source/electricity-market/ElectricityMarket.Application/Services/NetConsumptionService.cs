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
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.ElectricityMarket.Application.Services
{
    public class NetConsumptionService(ILogger<NetConsumptionService> logger) : INetConsumptionService
    {
        private static readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
        private static readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.SupplyToGrid, MeteringPointType.Consumption, MeteringPointType.ElectricalHeating, MeteringPointType.NetConsumption, MeteringPointType.ConsumptionFromGrid];

        private readonly Instant _cutOffDate = InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value;
        private readonly DateTimeOffset _endPeriodCutOffDate = new(DateTime.Today, DateTimeOffset.Now.Offset);
        private readonly SnakeCaseFormatter _snakeCaseFormatter = new();

        /// <summary>
        /// Consumption(parent) metering points in netsettlement group 6.
        /// The data is periodized; the following transaction types are relevant for determining the periods:
        /// - CHANGESUP: Leverandørskift(BRS-001)
        /// - ENDSUPPLY: Leveranceophør(BRS-002)
        /// - INCCHGSUP: Håndtering af fejlagtigt leverandørskift(BRS-003)
        /// - MSTDATSBM: Fremsendelse af stamdata(BRS-006) - Skift af nettoafregningsgrupper
        /// - MOVEINES: Tilflytning - meldt til elleverandøren(BRS-009)
        /// - MOVEOUTES: Fraflytning - meldt til elleverandøren(BRS-010)
        /// - INCMOVEAUT: Fejlagtig flytning - Automatisk(BRS-011)
        /// - INCMOVEMAN: Fejlagtig flytning - Manuel(BRS-011) HTX
        /// - MDCNSEHON: Oprettelse af elvarme(BRS-015) Det bliver til BRS-041 i DH3
        /// - MDCNSEHOFF: Fjernelse af elvarme(BRS-015) Det bliver til BRS-041 i DH3
        /// - CHGSUPSHRT: Leverandørskift med kort varsel(BRS-043). Findes ikke i DH3
        /// - MANCHGSUP: Tvunget leverandørskifte på målepunkt(BRS-044).
        /// - MANCOR(HTX) : Manuelt korrigering
        /// Periods are included when:
        /// - the parent metering point is in netsettlement group 6
        /// - the metering point physical status is connected or disconnected
        /// - the period does not end before 2021-01-01
        /// Formatting is according to ADR-144 with the following constraints:
        /// - No column may use quoted values
        /// - All date/time values must include seconds.
        /// </summary>
        public IReadOnlyList<NetConsumptionParentDto> GetParentNetConsumption(MeteringPointHierarchy meteringPointHierarchy)
        {
            ArgumentNullException.ThrowIfNull(meteringPointHierarchy);
            var parentMeteringPoint = meteringPointHierarchy.Parent;

            var segments = BuildMergedTimeline(parentMeteringPoint);

            var response = new List<NetConsumptionParentDto>();

            var electricalHeatingPeriods = parentMeteringPoint.CommercialRelationTimeline
                .SelectMany(cr => cr.ElectricalHeatingPeriods)
                .Select(ehp => ehp.Period)
                .Where(iv => iv.End > _cutOffDate)
                .ToList();

            var firstCustomerRelationByClient = parentMeteringPoint
                    .CommercialRelationTimeline
                        .Where(cr => cr.ClientId.HasValue)
                        .GroupBy(cr => cr.ClientId!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderBy(cr => cr.Period.Start).First().Period);

            foreach (var segment in segments.Where(s =>
                s.Metadata.Parent is null
                && s.Metadata.NetSettlementGroup == 6
                && s.Metadata.Type == MeteringPointType.Consumption
                && _relevantConnectionStates.Contains(s.Metadata.ConnectionState)
                && s.Period.End > _cutOffDate))
            {
                if (segment.Metadata.ScheduledMeterReadingMonth is null)
                {
                    logger.LogError(
                        "{ScheduledMeterReadingMonth} is null, which is not allowed for Net Settlement Group 6.",
                        segment.Metadata.ScheduledMeterReadingMonth);
                    continue;
                }

                var hasElectricalHeating = electricalHeatingPeriods
                    .Any(h => h.Start <= segment.Period.Start && segment.Period.End <= h.End);

                var isMoveIn = firstCustomerRelationByClient.Values.Any(fp =>
                    fp.Start == segment.Period.Start);

                var from = segment.Period.Start.ToDateTimeOffset();

                DateTimeOffset? to = segment.Period.End.ToDateTimeOffset();
                to = segment.Period.End == Instant.MaxValue || to > _endPeriodCutOffDate.ToUniversalTime()
                    ? null
                    : to;

                response.Add(new NetConsumptionParentDto(
                    MeteringPointId: parentMeteringPoint.Identification.Value,
                    HasElectricalHeating: hasElectricalHeating,
                    SettlementMonth: (int)segment.Metadata.ScheduledMeterReadingMonth,
                    PeriodFromDate: from,
                    PeriodToDate: to,
                    MoveIn: isMoveIn));
            }

            return response;
        }

        /// <summary>
        /// Child metering points related to electrical heating.
        ///  Periods are included when
        /// - the metering point is of type
        ///    'supply_to_grid' | 'consumption_from_grid' | 'electrical_heating' | 'net_consumption'
        /// - the metering point is coupled to a parent metering point
        ///    Note: The same child metering point cannot be re-coupled after being uncoupled
        /// - the child metering point physical status is connected or disconnected.
        /// - the period does not end before 2021-01-01
        /// Formatting is according to ADR-144 with the following constraints:
        /// - No column may use quoted values
        /// - All date/time values must include seconds.
        /// </summary>
        public IReadOnlyList<NetConsumptionChildDto> GetChildNetConsumption(MeteringPointHierarchy meteringPointHierarchy)
        {
            ArgumentNullException.ThrowIfNull(meteringPointHierarchy);
            var childMeteringPoints = meteringPointHierarchy.ChildMeteringPoints;

            var response = new List<NetConsumptionChildDto>();

            foreach (var child in childMeteringPoints)
            {
                var metadataTimelines = child.MetadataTimeline.Where(mt =>
                    mt.Parent is not null
                    && _relevantMeteringPointTypes.Contains(mt.Type)
                    && _relevantConnectionStates.Contains(mt.ConnectionState)
                    && mt.Valid.End > _cutOffDate);

                foreach (var metadataTimeline in metadataTimelines)
                {
                    var uncoupledDate = metadataTimeline.Valid.End.ToDateTimeOffset();

                    var netConsumptionChild = new NetConsumptionChildDto(
                        MeteringPointId: child.Identification.Value,
                        MeteringPointType: _snakeCaseFormatter.ToSnakeCase(metadataTimeline.Type.ToString()),
                        ParentMeteringPointId: metadataTimeline.Parent!.Value,
                        CoupledDate: metadataTimeline.Valid.Start.ToDateTimeOffset(),
                        UncoupledDate: uncoupledDate == DateTimeOffset.MaxValue || uncoupledDate > _endPeriodCutOffDate.ToUniversalTime() ? null : uncoupledDate);

                    response.Add(netConsumptionChild);
                }
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

            foreach (var cr in meteringPoint.CommercialRelationTimeline)
            {
                changePoints.Add(cr.Period.Start);
                if (cr.Period.End.ToDateTimeOffset() != DateTimeOffset.MaxValue)
                {
                    changePoints.Add(cr.Period.End);
                }
            }

            var builder = new TimelineBuilder();
            foreach (var start in changePoints)
            {
                var metadata = meteringPoint.MetadataTimeline
                    .First(m => m.Valid.Contains(start));
                var commercialRelation = meteringPoint.CommercialRelationTimeline
                    .FirstOrDefault(r => r.Period.Contains(start));

                builder.AddSegment(start, metadata, commercialRelation);
            }

            return builder.Build();
        }
    }
}
