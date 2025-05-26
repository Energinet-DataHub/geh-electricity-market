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

using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Domain.Models;

public sealed record MeteringPoint(
    long Id,
    DateTimeOffset Version,
    MeteringPointIdentification Identification,
    IReadOnlyList<MeteringPointMetadata> MetadataTimeline,
    IReadOnlyList<CommercialRelation> CommercialRelationTimeline)
{
    public MeteringPointMetadata Metadata
    {
        get
        {
            var selected = MetadataTimeline[0];
            var now = SystemClock.Instance.GetCurrentInstant();

            foreach (var metadata in MetadataTimeline)
            {
                if (metadata.Valid.Start > now)
                {
                    break;
                }

                selected = metadata;
            }

            return selected;
        }
    }

    public CommercialRelation? CommercialRelation
    {
        get
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            return CommercialRelationTimeline.FirstOrDefault(cr => cr.Period.Contains(now));
        }
    }

    public EnergySupplyPeriod? EnergySupplyPeriod
    {
        get
        {
            var now = SystemClock.Instance.GetCurrentInstant();
            return CommercialRelationTimeline.FirstOrDefault(cr => cr.Period.Contains(now))?
                .EnergySupplyPeriodTimeline.First(esp => esp.Valid.Contains(now));
        }
    }

    public bool IsParent()
    {
        return MetadataTimeline.All(mpp => mpp.Parent is null);
    }
}
