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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Models;

namespace Energinet.DataHub.ElectricityMarket.Application.Models;

public sealed class CommercialRelationDto
{
    public CommercialRelationDto(
        long id,
        long meteringPointId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string energySupplier,
        DateTimeOffset modifiedAt,
        IEnumerable<EnergySupplierPeriodDto> energySupplyPeriods,
        IEnumerable<ElectricalHeatingPeriodDto> electricalHeatingPeriods)
    {
        Id = id;
        MeteringPointId = meteringPointId;
        StartDate = startDate;
        EndDate = endDate;
        EnergySupplier = energySupplier;
        ModifiedAt = modifiedAt;
        EnergySupplyPeriods = energySupplyPeriods;
        ElectricalHeatingPeriods = electricalHeatingPeriods;
    }

    public long Id { get; init; }
    public long MeteringPointId { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public string EnergySupplier { get; init; }
    public DateTimeOffset ModifiedAt { get; init; }
    public IEnumerable<EnergySupplierPeriodDto> EnergySupplyPeriods { get; init; }
    public IEnumerable<ElectricalHeatingPeriodDto> ElectricalHeatingPeriods { get; init; }

    public ElectricalHeatingPeriodDto? CurrentElectricalHeatingPeriod =>
        ElectricalHeatingPeriods.FirstOrDefault(x => x.ValidFrom <= DateTimeOffset.Now && x.ValidTo >= DateTimeOffset.Now) ??
        ElectricalHeatingPeriods.OrderByDescending(x => x.ValidFrom).FirstOrDefault();

    public EnergySupplierPeriodDto? CurrentEnergySupplierPeriod =>
        EnergySupplyPeriods.FirstOrDefault(x => x.ValidFrom <= DateTimeOffset.Now && x.ValidTo >= DateTimeOffset.Now) ??
        EnergySupplyPeriods.OrderByDescending(x => x.ValidFrom).FirstOrDefault();
}
