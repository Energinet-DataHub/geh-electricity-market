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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;

namespace Energinet.DataHub.ElectricityMarket.Application.Services
{
    public class NetConsumptionService(
        IMeteringPointRepository meteringPointRepository,
        IBalanceResponsibleRepository balanceResponsibleRepository) : INetConsumptionService
    {
        private static readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
        private static readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.Consumption, MeteringPointType.Production];

        private readonly IMeteringPointRepository _meteringPointRepository = meteringPointRepository;
        private readonly IBalanceResponsibleRepository _balanceResponsibleRepository = balanceResponsibleRepository;

        public async Task<IEnumerable<NetConsumptionDto>> GetNetConsumptionMeteringPointsAsync(IAsyncEnumerable<MeteringPoint> meteringPoints)
        {
            ArgumentNullException.ThrowIfNull(meteringPoints);

            var candidates = await BuildCandidatesAsync(meteringPoints).ConfigureAwait(false);
            return FilterAndMergeCandidates(candidates);
        }

        private static bool IsRelevantConnectionStateAndType(MeteringPointMetadata meteringPointMetaData) =>
            _relevantConnectionStates.Contains(meteringPointMetaData.ConnectionState) &&
            _relevantMeteringPointTypes.Contains(meteringPointMetaData.Type);

        private static NetConsumptionDto CreateDto(
            MeteringPoint meteringPoint,
            MeteringPointMetadata meteringPointMetadata,
            string energySupplierId,
            string? balanceResponsiblePartyId)
        {
            return new NetConsumptionDto(
                MeteringPointId: meteringPoint.Identification.Value,
                Type: meteringPointMetadata.Type.ToString(),
                CalculationType: null, // TODO: Set this when we have the data available
                SettlementMethod: meteringPointMetadata.SettlementMethod?.ToString(),
                GridAreaCode: meteringPointMetadata.GridAreaCode,
                Resolution: meteringPointMetadata.Resolution,
                FromGridAreaCode: meteringPointMetadata.ExchangeFromGridAreaCode,
                ToGridAreaCode: meteringPointMetadata.ExchangeToGridAreaCode,
                Identification: meteringPointMetadata.Parent is null ? meteringPoint.Identification.Value : null,
                EnergySupplier: energySupplierId,
                BalanceResponsiblePartyId: balanceResponsiblePartyId,
                FromDate: meteringPointMetadata.Valid.Start.ToDateTimeOffset(),
                ToDate: meteringPointMetadata.Valid.End.ToDateTimeOffset());
        }

        private static bool TryFindCurrentRelation(
            MeteringPoint lookupPoint,
            MeteringPointMetadata mpm,
            [NotNullWhen(true)] out CommercialRelation? relation)
        {
            relation = lookupPoint.CommercialRelationTimeline
                .FirstOrDefault(cr =>
                    cr.Period.Start < mpm.Valid.End &&
                    mpm.Valid.Start < cr.Period.End &&
                    !string.IsNullOrEmpty(cr.EnergySupplier));

            return relation is not null;
        }

        private static List<NetConsumptionDto> FilterAndMergeCandidates(
            List<NetConsumptionDto> candidates)
        {
            var result = new List<NetConsumptionDto>();

            foreach (var group in candidates
                .Where(d => d.FromDate < d.ToDate)
                .GroupBy(d => d.MeteringPointId))
            {
                var sorted = group
                    .OrderBy(d => d.FromDate)
                    .ToList();

                if (sorted.Count == 0)
                    continue;

                result.Add(sorted[0]);

                for (var i = 1; i < sorted.Count; i++)
                {
                    if (sorted[i - 1].ToDate == sorted[i].FromDate)
                        result.Add(sorted[i]);
                }
            }

            return result;
        }

        private async Task<List<NetConsumptionDto>> BuildCandidatesAsync(
            IAsyncEnumerable<MeteringPoint> meteringPoints)
        {
            var candidates = new List<NetConsumptionDto>();
            var parentCache = new Dictionary<string, MeteringPoint?>();
            var balanceResponsiblePartyCache = new Dictionary<string, string>();

            await foreach (var mp in meteringPoints)
            {
                foreach (var mpm in mp.MetadataTimeline.Where(IsRelevantConnectionStateAndType))
                {
                    var lookupPoint = await ResolveLookupPointAsync(mpm.Parent, mp, parentCache).ConfigureAwait(false);

                    if (lookupPoint is null)
                        continue;

                    if (!TryFindCurrentRelation(lookupPoint, mpm, out var relation))
                        continue;

                    var energySupplierId = relation.EnergySupplier;
                    var balanceResponsiblePartyId = await ResolveBalanceResponsiblePartyAsync(
                                               energySupplierId,
                                               balanceResponsiblePartyCache)
                                               .ConfigureAwait(false);

                    candidates.Add(CreateDto(mp, mpm, energySupplierId, balanceResponsiblePartyId));
                }
            }

            return candidates;
        }

        private async Task<MeteringPoint?> ResolveLookupPointAsync(
            MeteringPointIdentification? parentId,
            MeteringPoint self,
            Dictionary<string, MeteringPoint?> parentCache)
        {
            if (parentId is null)
                return self;

            var key = parentId.Value;
            if (!parentCache.TryGetValue(key, out var parentMp))
            {
                parentMp = await _meteringPointRepository
                    .GetMeteringPointByIdAsync(parentId)
                    .ConfigureAwait(false);
                parentCache[key] = parentMp;
            }

            return parentMp;
        }

        private async Task<string?> ResolveBalanceResponsiblePartyAsync(
            string energySupplierId,
            Dictionary<string, string> cache)
        {
            if (!cache.TryGetValue(energySupplierId, out var balanceResponsiblePartyId))
            {
                balanceResponsiblePartyId = await _balanceResponsibleRepository
                    .GetBalanceResponsiblePartyIdByEnergySupplierAsync(energySupplierId)
                    .ConfigureAwait(false);

                if (balanceResponsiblePartyId is not null)
                    cache[energySupplierId] = balanceResponsiblePartyId;
            }

            return balanceResponsiblePartyId;
        }
    }
}
