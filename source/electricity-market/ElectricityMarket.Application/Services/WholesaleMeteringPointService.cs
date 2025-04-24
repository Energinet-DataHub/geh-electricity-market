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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Application.Services
{
    public class WholesaleMeteringPointService(
        IMeteringPointRepository meteringPointRepository,
        IBalanceResponsibleRepository balanceResponsibleRepository) : IWholesaleMeteringPointService
    {
        private readonly IMeteringPointRepository _meteringPointRepository = meteringPointRepository;
        private readonly IBalanceResponsibleRepository _balanceResponsibleRepository = balanceResponsibleRepository;

        private readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
        private readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.Consumption, MeteringPointType.Production];

        public async Task<IEnumerable<WholesaleMeteringPointDto>> GetWholesaleMeteringPointsAsync(IAsyncEnumerable<MeteringPoint> meteringPoints)
        {
            ArgumentNullException.ThrowIfNull(meteringPoints);

            var candidates = new List<WholesaleMeteringPointDto>();
            var parentCache = new Dictionary<string, MeteringPoint?>();
            var balanceResponsiblePartyCache = new Dictionary<string, string>();

            await foreach (var mp in meteringPoints)
            {
                foreach (var mpm in mp.MetadataTimeline
                    .Where(m =>
                        _relevantConnectionStates.Contains(m.ConnectionState) &&
                        _relevantMeteringPointTypes.Contains(m.Type)))
                {
                    var lookupPoint = mp;
                    if (mpm.Parent is not null)
                    {
                        if (!parentCache.TryGetValue(mpm.Parent.Value, out var parentMp))
                        {
                            parentMp = await _meteringPointRepository.GetMeteringPointByIdAsync(mpm.Parent).ConfigureAwait(false);
                            parentCache[mpm.Parent.Value] = parentMp;
                        }

                        if (parentMp is null)
                            continue; // parent not found => skip
                        lookupPoint = parentMp;
                    }

                    var relation = lookupPoint.CommercialRelationTimeline
                        .FirstOrDefault(cr =>
                            cr.Period.Start < mpm.Valid.End &&
                            mpm.Valid.Start < cr.Period.End &&
                            !string.IsNullOrEmpty(cr.EnergySupplier));
                    if (relation is null)
                        continue; // no supplier => skip

                    var energySupplierId = relation.EnergySupplier;

                    if (!balanceResponsiblePartyCache.TryGetValue(energySupplierId, out var balanceResponsiblePartyId))
                    {
                        balanceResponsiblePartyId = await _balanceResponsibleRepository
                            .GetBalanceResponsiblePartyIdByEnergySupplierAsync(energySupplierId)
                            .ConfigureAwait(false);

                        if (balanceResponsiblePartyId is not null)
                            balanceResponsiblePartyCache[energySupplierId] = balanceResponsiblePartyId;
                    }

                    candidates.Add(new WholesaleMeteringPointDto(
                        MeteringPointId: mp.Identification.Value,
                        Type: mpm.Type.ToString(),
                        CalculationType: null,
                        SettlementMethod: mpm.SettlementMethod?.ToString(),
                        GridAreaCode: mpm.GridAreaCode,
                        Resolution: mpm.Resolution,
                        FromGridAreaCode: mpm.ExchangeFromGridAreaCode,
                        ToGridAreaCode: mpm.ExchangeToGridAreaCode,
                        Identification: mpm.Parent is null ? mp.Identification.Value : null,
                        EnergySupplier: energySupplierId,
                        BalanceResponsiblePartyId: balanceResponsiblePartyId,
                        FromDate: mpm.Valid.Start.ToDateTimeOffset(),
                        ToDate: mpm.Valid.End.ToDateTimeOffset()));
                }
            }

            var result = new List<WholesaleMeteringPointDto>();

            foreach (var group in candidates
                .Where(d => d.FromDate < d.ToDate)
                .GroupBy(d => d.MeteringPointId))
            {
                var sorted = group.OrderBy(d => d.FromDate).ToList();
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
    }
}
