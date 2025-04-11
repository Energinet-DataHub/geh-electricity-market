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

using Energinet.DataHub.ElectricityMarket.Application.Commands.DeltaLakeSync;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers.DeltaLakeSync;

public sealed class SyncElectricalHeatingHandler : IRequestHandler<SyncElectricalHeatingCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly IElectricalHeatingPeriodizationService _electricalHeatingPeriodizationService;

    public SyncElectricalHeatingHandler(IMeteringPointRepository meteringPointRepository, ISyncJobsRepository syncJobsRepository, IElectricalHeatingPeriodizationService electricalHeatingPeriodizationService)
    {
        _meteringPointRepository = meteringPointRepository;
        _syncJobsRepository = syncJobsRepository;
        _electricalHeatingPeriodizationService = electricalHeatingPeriodizationService;
    }

    public async Task Handle(SyncElectricalHeatingCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.ElectricalHeating).ConfigureAwait(false);
        var meteringPointsToSync = _meteringPointRepository
            .GetMeteringPointsToSyncAsync(currentSyncJob.Version);

        var parentMeteringPoints = await _electricalHeatingPeriodizationService.GetParentElectricalHeatingAsync(meteringPointsToSync).ConfigureAwait(false);
        var childMeteringPoints = await _electricalHeatingPeriodizationService.GetChildElectricalHeatingAsync(meteringPointsToSync, parentMeteringPoints.Select(p => p.MeteringPointId)).ConfigureAwait(false);
        DateTimeOffset maxVersion = currentSyncJob.Version;
        await foreach (var meteringPoint in meteringPointsToSync)
        {
            // TODO: Implement the sync logic to Databricks for electrical heating metering points
            maxVersion = meteringPoint.Version > maxVersion ? meteringPoint.Version : maxVersion;
        }

        currentSyncJob = currentSyncJob with { Version = maxVersion };
        await _syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);
    }
}
