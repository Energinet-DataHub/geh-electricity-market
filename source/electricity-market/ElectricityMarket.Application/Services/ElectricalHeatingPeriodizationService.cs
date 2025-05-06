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

using Energinet.DataHub.ElectricityMarket.Application.Common;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class ElectricalHeatingPeriodizationService : IElectricalHeatingPeriodizationService
{
    private readonly string[] _relevantTransactionTypes =
    {
        TransactionTypes.ChangeSupplier,
        TransactionTypes.EndSupply,
        TransactionTypes.IncorrectSupplierChange,
        TransactionTypes.MasterDataSent,
        TransactionTypes.AttachChild,
        TransactionTypes.DettachChild,
        TransactionTypes.MoveIn,
        TransactionTypes.MoveOut,
        TransactionTypes.TransactionTypeIncMove,
        TransactionTypes.IncorrectMoveIn,
        TransactionTypes.ElectricalHeatingOn,
        TransactionTypes.ElectricalHeatingOff,
        TransactionTypes.ChangeSupplierShort,
        TransactionTypes.ManualChangeSupplier,
        TransactionTypes.ManualCorrections
    };
    private readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
    private readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.SupplyToGrid, MeteringPointType.ConsumptionFromGrid, MeteringPointType.ElectricalHeating, MeteringPointType.NetConsumption];
    private readonly Instant _cutoffDate = InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value;

    private readonly IMeteringPointRepository _meteringPointRepository;

    public ElectricalHeatingPeriodizationService(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

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
        var response = new List<ElectricalHeatingParentDto>();
        await foreach (var meteringPoint in meteringPoints.ConfigureAwait(false))
        {
            if (meteringPoint.CommercialRelationTimeline.Any(cr => cr.ElectricalHeatingPeriods.Any()))
            {
                var electricalHeatingTimeline = meteringPoint.CommercialRelationTimeline.SelectMany(cr => cr.ElectricalHeatingPeriods);
                var metadataTimeline = meteringPoint.MetadataTimeline
                    .Where(mt => mt.Parent is null && mt.Type == MeteringPointType.Consumption // Consumption (parent) metering points
                        && _relevantTransactionTypes.Contains(mt.TransactionType.Trim()) // the following transaction types are relevant for determining the periods
                        && mt.SubType == MeteringPointSubType.Physical && _relevantConnectionStates.Contains(mt.ConnectionState) // the metering point physical status is connected or disconnected
                        && mt.Valid.End > _cutoffDate); // the period does not end before 2021-01-01

                foreach (var electricalHeatingPeriod in electricalHeatingTimeline)
                {
                    var metadataInPeriod = metadataTimeline
                        .Where(m => m.Valid.Start >= electricalHeatingPeriod.Period.Start && m.Valid.End <= electricalHeatingPeriod.Period.End);
                    foreach (var meteringPointMetadata in metadataInPeriod)
                    {
                        if (meteringPointMetadata.Valid.Start >= electricalHeatingPeriod.Period.Start
                            && meteringPointMetadata.Valid.End <= electricalHeatingPeriod.Period.End)
                        {
                            var electricalHeatingParent = new ElectricalHeatingParentDto(
                                meteringPoint.Identification.Value,
                                meteringPointMetadata.NetSettlementGroup,
                                meteringPointMetadata.NetSettlementGroup == 6 ? meteringPointMetadata.ScheduledMeterReadingMonth : 1,
                                meteringPointMetadata.Valid.Start.ToDateTimeOffset(),
                                meteringPointMetadata.Valid.End.ToDateTimeOffset());
                            response.Add(electricalHeatingParent);
                        }
                    }
                }
            }
        }

        return response;
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
    public async Task<IEnumerable<ElectricalHeatingChildDto>> GetChildElectricalHeatingAsync(IEnumerable<string> parentMeteringPointIds)
    {
        ArgumentNullException.ThrowIfNull(parentMeteringPointIds);
        var response = new List<ElectricalHeatingChildDto>();
        foreach (var parentId in parentMeteringPointIds)
        {
            var childMeteringPoints = await _meteringPointRepository.GetChildMeteringPointsAsync(parentId).ConfigureAwait(false);
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
                        metadataTimeline.Type.ToString(),
                        metadataTimeline.SubType.ToString(),
                        metadataTimeline.Parent!.Value,
                        metadataTimeline.Valid.Start.ToDateTimeOffset(),
                        metadataTimeline.Valid.End.ToDateTimeOffset());
                    response.Add(electricalHeatingChild);
                }
            }
        }

        return response;
    }
}
