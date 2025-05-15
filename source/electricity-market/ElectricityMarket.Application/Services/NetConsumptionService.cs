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
using Energinet.DataHub.ElectricityMarket.Application.Handlers.DeltaLakeSync;
using Energinet.DataHub.ElectricityMarket.Application.Helpers;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.ElectricityMarket.Application.Services
{
    public class NetConsumptionService(IMeteringPointRepository meteringPointRepository, ILogger<NetConsumptionService> logger) : INetConsumptionService
    {
        private static readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
        private static readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.SupplyToGrid, MeteringPointType.Consumption, MeteringPointType.ElectricalHeating, MeteringPointType.NetConsumption];
        private static readonly string[] _relevantTransactionTypes =
            [TransactionTypes.ChangeSupplier,
            TransactionTypes.EndSupply,
            TransactionTypes.IncorrectSupplierChange,
            TransactionTypes.MasterDataSent,
            TransactionTypes.MoveIn,
            TransactionTypes.MoveOut,
            TransactionTypes.TransactionTypeIncMove,
            TransactionTypes.IncorrectMoveIn,
            TransactionTypes.ElectricalHeatingOn,
            TransactionTypes.ElectricalHeatingOff,
            TransactionTypes.ChangeSupplierShort,
            TransactionTypes.ManualChangeSupplier,
            TransactionTypes.ManualCorrections];

        private readonly Instant _cutOffDate = InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value;
        private readonly SnakeCaseFormatter _snakeCaseFormatter = new();

        private readonly IMeteringPointRepository _meteringPointRepository = meteringPointRepository;
        private readonly ILogger<NetConsumptionService> _logger = logger;

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
        public IReadOnlyList<NetConsumptionParentDto> GetParentNetConsumption(MeteringPoint meteringPoint)
        {
            ArgumentNullException.ThrowIfNull(meteringPoint);

            var response = new List<NetConsumptionParentDto>();

            var metadataTimeline = meteringPoint.MetadataTimeline
                .Where(mt => mt.Parent is null
                && mt.NetSettlementGroup == 6
                && mt.Type == MeteringPointType.Consumption
                && _relevantConnectionStates.Contains(mt.ConnectionState)
                && _relevantTransactionTypes.Contains(mt.TransactionType)
                && mt.Valid.End > _cutOffDate);

            var electricalHeatingPeriods = meteringPoint.CommercialRelationTimeline
                .SelectMany(cr => cr.ElectricalHeatingPeriods)
                .Select(ehp => ehp.Period)
                .Where(iv => iv.End > _cutOffDate)
                .ToList();

            foreach (var meteringPointMetadata in metadataTimeline)
            {
                if (meteringPointMetadata.ScheduledMeterReadingMonth is null)
                {
                    _logger.LogError(
                        "{ScheduledMeterReadingMonth} is null, which is not allowed for Net Settlement Group 6.",
                        meteringPointMetadata.ScheduledMeterReadingMonth);
                    continue;
                }

                var hasElectricalHeating = electricalHeatingPeriods
                    .Any(h => h.Start < meteringPointMetadata.Valid.End && meteringPointMetadata.Valid.Start < h.End);

                var periodToDate = meteringPointMetadata.Valid.End.ToDateTimeOffset();

                var netConsumptionParent = new NetConsumptionParentDto(
                    MeteringPointId: meteringPoint.Identification.Value,
                    HasElectricalHeating: hasElectricalHeating,
                    SettlementMonth: (int)meteringPointMetadata.ScheduledMeterReadingMonth,
                    PeriodFromDate: meteringPointMetadata.Valid.Start.ToDateTimeOffset(),
                    PeriodToDate: periodToDate == DateTimeOffset.MaxValue ? null : periodToDate,
                    MoveIn: meteringPointMetadata.TransactionType == TransactionTypes.MoveIn);

                response.Add(netConsumptionParent);
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
        public async Task<IReadOnlyList<NetConsumptionChildDto>> GetChildNetConsumptionAsync(IEnumerable<long> parentMeteringPointIds)
        {
            ArgumentNullException.ThrowIfNull(parentMeteringPointIds);

            var response = new List<NetConsumptionChildDto>();

            foreach (var parentId in parentMeteringPointIds)
            {
                var childMeteringPoints = await _meteringPointRepository.GetChildMeteringPointsAsync(parentId).ConfigureAwait(false);

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
                            UncoupledDate: uncoupledDate == DateTimeOffset.MaxValue ? null : uncoupledDate);

                        response.Add(netConsumptionChild);
                    }
                }
            }

            return response;
        }
    }
}
