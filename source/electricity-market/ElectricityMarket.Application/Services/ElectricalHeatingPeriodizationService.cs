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
using NodaTime.Text;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class ElectricalHeatingPeriodizationService : IElectricalHeatingPeriodizationService
{
    private readonly string[] _relevantTransactionTypes = ["CHANGESUP", "ENDSUPPLY", "INCCHGSUP", "MSTDATSBM", "LNKCHLDMP", "ULNKCHLDMP", "ULNKCHLDMP", "MOVEINES", "MOVEOUTES", "INCMOVEAUT", "INCMOVEMAN", "MDCNSEHON", "MDCNSEHOFF", "CHGSUPSHRT", "MANCHGSUP", "MANCOR"];
    private readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
    private readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.SupplyToGrid, MeteringPointType.ConsumptionFromGrid, MeteringPointType.ElectricalHeating, MeteringPointType.NetConsumption];

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
    public async Task<IEnumerable<ElectricalHeatingParentDto>> GetParentElectricalHeatingAsync(IAsyncEnumerable<MeteringPoint> meteringPoints)
    {
        ArgumentNullException.ThrowIfNull(meteringPoints);
        var meteringPointsWithElectricalHeating = new List<MeteringPoint>();
        await foreach (var meteringPoint in meteringPoints.ConfigureAwait(false))
        {
            if (meteringPoint.CommercialRelationTimeline.Any(cr => cr.ElectricalHeatingPeriods.Any()))
            {
                meteringPointsWithElectricalHeating.Add(meteringPoint);
            }
        }

        if (meteringPointsWithElectricalHeating.Count == 0)
        {
            return [];
        }

        var parentMeteringPointPeriods = meteringPointsWithElectricalHeating
            .SelectMany(mp => mp.MetadataTimeline.Where(
                mpm => mpm.Parent is null && mpm.Type == MeteringPointType.Consumption // Consumption (parent) metering points
                && _relevantTransactionTypes.Contains(mpm.TransactionType.Trim()) // the following transaction types are relevant for determining the periods
                && mpm.SubType == MeteringPointSubType.Physical && _relevantConnectionStates.Contains(mpm.ConnectionState) // the metering point physical status is connected or disconnected
                && mpm.Valid.End > InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value) // the period does not end before 2021-01-01
            .Select(mpm => new { MeteringPoint = mp, MeteringPointPeriod = mpm }));

        return parentMeteringPointPeriods.Select(x => MapToParent(x.MeteringPoint, x.MeteringPointPeriod));
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
    public async Task<IEnumerable<ElectricalHeatingChildDto>> GetChildElectricalHeatingAsync(IAsyncEnumerable<MeteringPoint> allMeteringPoints, IEnumerable<string> parentMeteringPointIds)
    {
        var childMeteringPoints = await allMeteringPoints.Where(mp => parentMeteringPointIds.Contains(mp.Identification.Value))
            .ToListAsync().ConfigureAwait(false);
        var childMeteringPointPeriods = childMeteringPoints
            .SelectMany(cmp => cmp.MetadataTimeline.Where(
                mpm => _relevantMeteringPointTypes.Contains(mpm.Type) // the metering point is of following types
                && mpm.SubType == MeteringPointSubType.Physical && _relevantConnectionStates.Contains(mpm.ConnectionState) // the child metering point physical status is connected or disconnected
                && mpm.Valid.End > InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value) // the period does not end before 2021-01-01
            .Select(mpm => new { MeteringPoint = cmp, MeteringPointPeriod = mpm }));

        return childMeteringPointPeriods.Select(x => MapToChild(x.MeteringPoint, x.MeteringPointPeriod));
    }

    private static bool HasElectricalHeating(MeteringPointMetadata meteringPointMetadata, MeteringPoint meteringPoint)
    {
        var commertialRelationsRelatedToMP = meteringPoint.CommercialRelationTimeline.Where(cr => DoDatesOverlap(meteringPointMetadata.Valid, cr.Period));
        return commertialRelationsRelatedToMP.Any(cr => cr.ElectricalHeatingPeriods.Any());
    }

    private static bool DoDatesOverlap(Interval interval1, Interval interval2)
    {
        return interval1.Start < interval2.End && interval2.Start < interval1.End;
    }

    private static ElectricalHeatingParentDto MapToParent(MeteringPoint meteringPoint, MeteringPointMetadata meteringPointMetadata)
    {
        return new ElectricalHeatingParentDto(
            meteringPoint.Identification.Value,
            HasElectricalHeating(meteringPointMetadata, meteringPoint),
            meteringPointMetadata.NetSettlementGroup,
            meteringPointMetadata.NetSettlementGroup == 6 ? meteringPointMetadata.ScheduledMeterReadingMonth : 1,
            meteringPointMetadata.Valid.Start.ToDateTimeOffset(),
            meteringPointMetadata.Valid.End.ToDateTimeOffset())
        ;
    }

    private static ElectricalHeatingChildDto MapToChild(MeteringPoint meteringPoint, MeteringPointMetadata meteringPointPeriod)
    {
        return new ElectricalHeatingChildDto(
            meteringPoint.Identification.Value,
            meteringPointPeriod.Type.ToString(),
            meteringPointPeriod.SubType.ToString(),
            meteringPointPeriod.Parent!.Value,
            meteringPointPeriod.Valid.Start.ToDateTimeOffset(),
            meteringPointPeriod.Valid.HasEnd ? meteringPointPeriod.Valid.End.ToDateTimeOffset() : null);
    }
}
