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
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;

namespace Energinet.DataHub.ElectricityMarket.Application.Services
{
    public class NetConsumptionService(IMeteringPointRepository meteringPointRepository) : INetConsumptionService
    {
        private static readonly MeteringPointType[] _relevantTypes = [MeteringPointType.SupplyToGrid, MeteringPointType.Consumption, MeteringPointType.NetConsumption];
        private readonly IMeteringPointRepository _meteringPointRepository = meteringPointRepository;

        public async Task<(IEnumerable<NetConsumptionParentDto> Parents, IEnumerable<NetConsumptionChildDto> Children)>
            GetNetConsumptionMeteringPointsAsync(IAsyncEnumerable<MeteringPoint> meteringPoints)
        {
            ArgumentNullException.ThrowIfNull(meteringPoints);

            var children = new List<NetConsumptionChildDto>();
            var parentIds = new HashSet<string>();

            await foreach (var mp in meteringPoints)
            {
                foreach (var mpm in mp.MetadataTimeline
                    .Where(x => x.Parent is not null && IsRelevantType(x)))
                {
                    children.Add(new NetConsumptionChildDto(
                        MeteringPointId: mp.Identification.Value,
                        MeteringPointType: mpm.Type.ToString(),
                        ParentMeteringPointId: mpm.Parent!.Value,
                        CoupledDate: mpm.Valid.Start.ToDateTimeOffset(),
                        UncoupledDate: mpm.Valid.End.ToDateTimeOffset()));

                    parentIds.Add(mpm.Parent.Value);
                }
            }

            var parents = new List<NetConsumptionParentDto>();
            var hierarchyCache = new Dictionary<string, MeteringPointHierarchy>();

            foreach (var parentId in parentIds)
            {
                if (!hierarchyCache.TryGetValue(parentId, out var hierarchy))
                {
                    hierarchy = await _meteringPointRepository
                        .GetMeteringPointHierarchyAsync(
                            new MeteringPointIdentification(parentId))
                        .ConfigureAwait(false);

                    hierarchyCache[parentId] = hierarchy;
                }

                var parentMp = hierarchy.Parent;

                var hasHeating = parentMp.CommercialRelationTimeline
                    .SelectMany(cr => cr.ElectricalHeatingPeriods)
                    .Any();

                if (hasHeating)
                    continue;

                var latestMeta = parentMp.MetadataTimeline
                    .OrderByDescending(x => x.Valid.Start)
                    .First();

                var fromDate = latestMeta.Valid.Start.ToDateTimeOffset();
                var toDate = latestMeta.Valid.End.ToDateTimeOffset();

                var settlementMonth = latestMeta.ScheduledMeterReadingMonth
                    ?? fromDate.Month;

                var moveIn = parentMp.CommercialRelationTimeline
                    .Any(cr => cr.ClientId != null);

                parents.Add(new NetConsumptionParentDto(
                    MeteringPointId: parentId,
                    HasElectricalHeating: false,
                    SettlementMonth: settlementMonth,
                    PeriodFromDate: fromDate,
                    PeriodToDate: toDate,
                    MoveIn: moveIn));
            }

            return (Parents: parents, Children: children);
        }

        private static bool IsRelevantType(MeteringPointMetadata mpm) =>
            _relevantTypes.Contains(mpm.Type);
    }
}
