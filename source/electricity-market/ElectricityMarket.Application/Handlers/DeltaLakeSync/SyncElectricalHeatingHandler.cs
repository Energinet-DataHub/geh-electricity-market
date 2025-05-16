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

        var moreData = true;
        while (moreData)
        {
            _logger.LogWarning(
                "SyncElectricalHeatingHandler: Sync job version {Version} for {JobName} started.",
                currentSyncJob.Version,
                SyncJobName.ElectricalHeating);
            var meteringPointsToSync = _meteringPointRepository
                .GetMeteringPointsToSyncAsync(currentSyncJob.Version);

            var maxVersion = await HandleBatchAsync(meteringPointsToSync).ConfigureAwait(false);
            if (maxVersion > DateTimeOffset.MinValue)
            {
                currentSyncJob = currentSyncJob with { Version = maxVersion };
                await _syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);
            }
            else
            {
                moreData = false;
            }
        }
    }

    private async Task<DateTimeOffset> HandleBatchAsync(IAsyncEnumerable<MeteringPoint> meteringPointsToSync)
    {
        var maxVersion = DateTimeOffset.MinValue;
        await foreach (var meteringPoint in meteringPointsToSync.ConfigureAwait(false))
        {
            maxVersion = meteringPoint.Version > maxVersion ? meteringPoint.Version : maxVersion;
            var parentMeteringPoints = _electricalHeatingPeriodizationService.GetParentElectricalHeating(meteringPoint);
            if (parentMeteringPoints.Any())
            {
                await _deltaLakeDataUploadService.ImportTransactionsAsync(parentMeteringPoints).ConfigureAwait(false);

                var childMeteringPoints = await _electricalHeatingPeriodizationService.GetChildElectricalHeatingAsync(parentMeteringPoints.Select(p => p.MeteringPointId).Distinct()).ConfigureAwait(false);
                if (childMeteringPoints.Any())
                {
                    await _deltaLakeDataUploadService.ImportTransactionsAsync(childMeteringPoints).ConfigureAwait(false);
                }
            }
        }

        return maxVersion;
    }
}
