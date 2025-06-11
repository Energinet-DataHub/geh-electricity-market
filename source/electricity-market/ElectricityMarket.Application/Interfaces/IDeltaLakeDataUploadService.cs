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

namespace Energinet.DataHub.ElectricityMarket.Application.Interfaces;

public interface IDeltaLakeDataUploadService
{
    Task InsertParentElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingParentDto> electricalHeatingParent);

    Task InsertChildElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingChildDto> electricalHeatingChildren);

    Task DeleteParentElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingEmptyDto> electricalHeatingEmpty);

    Task DeleteChildElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingEmptyDto> electricalHeatingEmpty);

    Task InsertCapacitySettlementPeriodsAsync(IReadOnlyList<CapacitySettlementPeriodDto> capacitySettlementPeriods, CancellationToken cancellationToken);

    Task DeleteCapacitySettlementPeriodsAsync(IReadOnlyList<CapacitySettlementEmptyDto> capacitySettlementEmptyDtos, CancellationToken cancellationToken);

    Task ImportTransactionsAsync(IReadOnlyList<NetConsumptionParentDto> netConsumptionParents);

    Task ImportTransactionsAsync(IReadOnlyList<NetConsumptionChildDto> netConsumptionChildren);

    Task InsertHullerLogPeriodsAsync(IReadOnlyList<HullerLogDto> hullerLogs);

    Task DeleteHullerLogPeriodsAsync(IReadOnlyList<HullerLogEmptyDto> emptyHullerLogs);

    Task InsertMeasurementsReportPeriodsAsync(IReadOnlyList<MeasurementsReportDto> measurementsReports);

    Task DeleteMeasurementsReportPeriodsAsync(IReadOnlyList<MeasurementsReportEmptyDto> emptyMeasurementsReports);
}
