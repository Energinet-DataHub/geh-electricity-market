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

namespace Energinet.DataHub.ElectricityMarket.Application.Models;

public sealed class MeteringPointDto
{
    public MeteringPointDto(
        long id,
        string identification,
        IEnumerable<MeteringPointPeriodDto> meteringPointPeriod,
        IEnumerable<CommercialRelationDto> commercialRelations)
    {
        Id = id;
        Identification = identification;
        MeteringPointPeriod = meteringPointPeriod;
        CommercialRelations = commercialRelations;
    }

    public long Id { get; init; }
    public string Identification { get; init; }
    public IEnumerable<MeteringPointPeriodDto> MeteringPointPeriod { get; init; }
    public IEnumerable<CommercialRelationDto> CommercialRelations { get; init; }

    public MeteringPointPeriodDto? CurrentMeteringPointPeriod =>
        MeteringPointPeriod.FirstOrDefault(x => x.ValidFrom <= DateTimeOffset.Now && x.ValidTo >= DateTimeOffset.Now) ??
        MeteringPointPeriod.OrderByDescending(x => x.ValidFrom).FirstOrDefault();

    public CommercialRelationDto? CurrentCommercialRelation =>
        CommercialRelations.FirstOrDefault(x => x.StartDate <= DateTimeOffset.Now && x.EndDate >= DateTimeOffset.Now) ??
        CommercialRelations.OrderByDescending(x => x.StartDate).FirstOrDefault();
}
