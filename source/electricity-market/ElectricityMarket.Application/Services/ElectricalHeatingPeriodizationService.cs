﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class ElectricalHeatingPeriodizationService : IElectricalHeatingPeriodizationService
{
    private readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
    private readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.SupplyToGrid, MeteringPointType.ConsumptionFromGrid, MeteringPointType.ElectricalHeating, MeteringPointType.NetConsumption];
    private readonly Instant _cutoffDate = InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value;
    private readonly SnakeCaseFormatter _snakeCaseFormatter = new();

    /// <summary>
    /// Consumption (parent) metering points related to electrical heating. <br/>
    /// The data is periodized; the following transaction types are relevant for determining the periods:<br/>
    /// - CHANGESUP: Leverandørskift (BRS-001)<br/>
    /// - ENDSUPPLY: Leveranceophør (BRS-002)<br/>
    /// - INCCHGSUP: Håndtering af fejlagtigt leverandørskift (BRS-003)<br/>
    /// - MSTDATSBM: Fremsendelse af stamdata (BRS-006) - Skift af nettoafregningsgrupper<br/>
    /// - LNKCHLDMP: Tilkobling af D15 til parent i nettoafregningsgruppe 2<br/>
    /// - ULNKCHLDMP: Afkobling af D15 af parent i nettoafregningsgruppe 2<br/>
    /// - ULNKCHLDMP: Afkobling af D14 af parent<br/>
    /// - MOVEINES: Tilflytning - meldt til elleverandøren (BRS-009)<br/>
    /// - MOVEOUTES: Fraflytning - meldt til elleverandøren (BRS-010)<br/>
    /// - INCMOVEAUT: Fejlagtig flytning - Automatisk (BRS-011)<br/>
    /// - INCMOVEMAN: Fejlagtig flytning - Manuel (BRS-011) HTX<br/>
    /// - MDCNSEHON: Oprettelse af elvarme (BRS-015) Det bliver til BRS-041 i DH3<br/>
    /// - MDCNSEHOFF: Fjernelse af elvarme (BRS-015) Det bliver til BRS-041 i DH3<br/>
    /// - CHGSUPSHRT: Leverandørskift med kort varsel (BRS-043). Findes ikke i DH3<br/>
    /// - MANCHGSUP: Tvunget leverandørskifte på målepunkt (BRS-044).<br/>
    /// - MANCOR (HTX): Manuelt korrigering<br/>
    /// Periods are  included when<br/>
    /// - the metering point physical status is connected or disconnected<br/>
    /// - the period does not end before 2021-01-01<br/>
    /// - the electrical heating is or has been registered for the period.
    /// </summary>
    public IEnumerable<ElectricalHeatingParentDto> GetParentElectricalHeating(MeteringPoint meteringPoint)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        if (meteringPoint.CommercialRelationTimeline.Any(cr => cr.ElectricalHeatingPeriods.Any()))
        {
            var electricalHeatingTimeline = meteringPoint.CommercialRelationTimeline
                .SelectMany(cr => cr.ElectricalHeatingPeriods)
                .Where(ehp => ehp.IsActive && ehp.Period.End > _cutoffDate);
            var metadataTimeline = meteringPoint.MetadataTimeline
                .Where(mt => mt.Parent is null && mt.Type == MeteringPointType.Consumption // Consumption (parent) metering points
                   && _relevantConnectionStates.Contains(mt.ConnectionState) // the metering point physical status is connected or disconnected
                    && mt.Valid.End > _cutoffDate).ToList(); // the period does not end before 2021-01-01

            foreach (var electricalHeatingPeriod in electricalHeatingTimeline)
            {
                var metadataInPeriod = metadataTimeline
                    .Where(m => m.Valid.Start < electricalHeatingPeriod.Period.End && electricalHeatingPeriod.Period.Start < m.Valid.End);
                foreach (var meteringPointMetadata in metadataInPeriod)
                {
                    var start = meteringPointMetadata.Valid.Start;
                    var end = meteringPointMetadata.Valid.End;
                    if (meteringPointMetadata.Valid.Start < electricalHeatingPeriod.Period.Start)
                    {
                        start = electricalHeatingPeriod.Period.Start;
                    }

                    if (meteringPointMetadata.Valid.End > electricalHeatingPeriod.Period.End)
                    {
                        end = electricalHeatingPeriod.Period.End;
                    }

                    var interval = new Interval(start, end);
                    var electricalHeatingParent = CreateParent(meteringPoint.Identification.Value, meteringPointMetadata, interval);
                    yield return electricalHeatingParent;
                }
            }
        }
    }

    /// <summary>
    /// Child metering points related to electrical heating.
    /// Periods are included when
    /// - the metering point is of type
    ///   'supply_to_grid' | 'consumption_from_grid' | 'electrical_heating' | 'net_consumption'
    /// - the metering point is coupled to a parent metering point
    ///   Note: The same child metering point cannot be re-coupled after being uncoupled
    /// - the child metering point physical status is connected or disconnected.
    /// - the period does not end before 2021-01-01.
    /// </summary>
    ///
    public IEnumerable<ElectricalHeatingChildDto> GetChildElectricalHeating(IEnumerable<MeteringPoint> childMeteringPoints)
    {
        ArgumentNullException.ThrowIfNull(childMeteringPoints);
        foreach (var child in childMeteringPoints)
        {
            var metadataTimelines = child.MetadataTimeline.Where(mt =>
            mt.Parent is not null &&
            _relevantMeteringPointTypes.Contains(mt.Type) // the metering point is of following types
            && _relevantConnectionStates.Contains(mt.ConnectionState) // the child metering point physical status is connected or disconnected
            && mt.Valid.End > _cutoffDate); // the period does not end before 2021-01-01

            foreach (var metadataTimeline in metadataTimelines)
            {
                var electricalHeatingChild = new ElectricalHeatingChildDto(
                    child.Identification.Value,
                    _snakeCaseFormatter.ToSnakeCase(metadataTimeline.Type.ToString()),
                    _snakeCaseFormatter.ToSnakeCase(metadataTimeline.SubType.ToString()),
                    metadataTimeline.Parent!.Value,
                    metadataTimeline.Valid.Start.ToDateTimeOffset(),
                    metadataTimeline.Valid.End.ToDateTimeOffset());
                yield return electricalHeatingChild;
            }
        }
    }

    private static ElectricalHeatingParentDto CreateParent(long identification, MeteringPointMetadata metadata, Interval period)
        => new(identification, metadata.NetSettlementGroup, metadata.NetSettlementGroup == 6 ? metadata.ScheduledMeterReadingMonth : 1, period.Start.ToDateTimeOffset(), period.End.ToDateTimeOffset());
}
