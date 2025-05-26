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
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers.DeltaLakeSync;

public class SyncCapacitySettlementHandler : IRequestHandler<SyncCapacitySettlementCommand>
{
    private const int BatchSize = 50;

    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly ICapacitySettlementService _capacitySettlementService;
    private readonly IDeltaLakeDataUploadService _deltaLakeDataUploadService;
    private readonly ILogger<SyncCapacitySettlementHandler> _logger;

    public SyncCapacitySettlementHandler(
        IMeteringPointRepository meteringPointRepository,
        ISyncJobsRepository syncJobsRepository,
        ICapacitySettlementService capacitySettlementService,
        IDeltaLakeDataUploadService deltaLakeDataUploadService,
        ILogger<SyncCapacitySettlementHandler> logger)
    {
        _meteringPointRepository = meteringPointRepository;
        _syncJobsRepository = syncJobsRepository;
        _capacitySettlementService = capacitySettlementService;
        _deltaLakeDataUploadService = deltaLakeDataUploadService;
        _logger = logger;
    }

    public async Task Handle(SyncCapacitySettlementCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.CapacitySettlement).ConfigureAwait(false);

        var moreData = true;
        while (moreData)
        {
            _logger.LogWarning(
                "SyncCapacitySettlementHandler: Sync job version {Version} for {JobName} started.",
                currentSyncJob.Version,
                SyncJobName.CapacitySettlement);

            var meteringPointsToSync = _meteringPointRepository.GetMeteringPointsToSyncAsync(currentSyncJob.Version, BatchSize);
            var maxVersionProcessed = await HandleBatchAsync(meteringPointsToSync, cancellationToken).ConfigureAwait(false);
            if (maxVersionProcessed is not null)
            {
                currentSyncJob = currentSyncJob with { Version = maxVersionProcessed.Value };
                await _syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);
            }
            else
            {
                moreData = false;
            }
        }
    }

    private async Task<DateTimeOffset?> HandleBatchAsync(IAsyncEnumerable<MeteringPoint> meteringPointsToSync, CancellationToken cancellationToken)
    {
        DateTimeOffset? maxVersion = null;

        var meteringPoints = await meteringPointsToSync.SelectMany(
            mp =>
            {
                maxVersion = maxVersion is null || maxVersion < mp.Version ? mp.Version : maxVersion;
                return _capacitySettlementService.GetCapacitySettlementPeriodsAsync(mp, cancellationToken);
            }).ToListAsync(cancellationToken).ConfigureAwait(false);

        var meteringPointsToInsert = meteringPoints.OfType<CapacitySettlementPeriodDto>().ToList();
        var meteringPointsToDelete = meteringPoints.ToList();

        if (meteringPointsToDelete.Count > 0)
        {
            await _deltaLakeDataUploadService
                .DeleteCapacitySettlementPeriodsAsync(meteringPointsToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        if (meteringPointsToInsert.Count > 0)
        {
            await _deltaLakeDataUploadService
                .InsertCapacitySettlementPeriodsAsync(meteringPointsToInsert, cancellationToken)
                .ConfigureAwait(false);
        }

        return maxVersion;
    }
}
