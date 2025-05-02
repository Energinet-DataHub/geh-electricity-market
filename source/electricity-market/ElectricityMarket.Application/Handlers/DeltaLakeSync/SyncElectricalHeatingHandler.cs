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
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers.DeltaLakeSync;

public sealed class SyncElectricalHeatingHandler : IRequestHandler<SyncElectricalHeatingCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly IElectricalHeatingPeriodizationService _electricalHeatingPeriodizationService;
    private readonly IDeltaLakeDataUploadService _deltaLakeDataUploadService;
    private readonly ILogger<SyncElectricalHeatingHandler> _logger;

    public SyncElectricalHeatingHandler(
        IMeteringPointRepository meteringPointRepository,
        ISyncJobsRepository syncJobsRepository,
        IElectricalHeatingPeriodizationService electricalHeatingPeriodizationService,
        IDeltaLakeDataUploadService deltaLakeDataUploadService,
        ILogger<SyncElectricalHeatingHandler> logger)
    {
        _meteringPointRepository = meteringPointRepository;
        _syncJobsRepository = syncJobsRepository;
        _electricalHeatingPeriodizationService = electricalHeatingPeriodizationService;
        _deltaLakeDataUploadService = deltaLakeDataUploadService;
        _logger = logger;
    }

    public async Task Handle(SyncElectricalHeatingCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.ElectricalHeating).ConfigureAwait(false);
        _logger.LogInformation(
            "SyncElectricalHeatingHandler: Sync job version {Version} for {JobName} started.",
            currentSyncJob.Version,
            SyncJobName.ElectricalHeating);

        var meteringPointsToSync = _meteringPointRepository
            .GetMeteringPointsToSyncAsync(currentSyncJob.Version);

        while (await meteringPointsToSync.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var maxVersion = await meteringPointsToSync.MaxAsync(mp => mp.Version, cancellationToken).ConfigureAwait(false);
            await HandleBatchAsync(meteringPointsToSync).ConfigureAwait(false);
            currentSyncJob = currentSyncJob with { Version = maxVersion };
            await _syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);

            _logger.LogInformation(
                "SyncElectricalHeatingHandler: Sync job version {Version} for {JobName} started.",
                currentSyncJob.Version,
                SyncJobName.ElectricalHeating);
            meteringPointsToSync = _meteringPointRepository.GetMeteringPointsToSyncAsync(currentSyncJob.Version);
        }
    }

    private async Task HandleBatchAsync(IAsyncEnumerable<MeteringPoint> meteringPointsToSync)
    {
        var parentMeteringPoints = await _electricalHeatingPeriodizationService.GetParentElectricalHeatingAsync(meteringPointsToSync).ConfigureAwait(false);
        await _deltaLakeDataUploadService.ImportTransactionsAsync(parentMeteringPoints).ConfigureAwait(false);
        var childMeteringPoints = await _electricalHeatingPeriodizationService.GetChildElectricalHeatingAsync(parentMeteringPoints.Select(p => p.MeteringPointId).Distinct()).ConfigureAwait(false);
        await _deltaLakeDataUploadService.ImportTransactionsAsync(childMeteringPoints).ConfigureAwait(false);
    }
}
