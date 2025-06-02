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

public sealed class SyncHullerLogHandler(
    IMeteringPointRepository meteringPointRepository,
    ISyncJobsRepository syncJobsRepository,
    IHullerLogService hullerLogService,
    IDeltaLakeDataUploadService deltaLakeDataUploadService,
    ILogger<SyncHullerLogHandler> logger) : IRequestHandler<SyncHullerLogCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository = meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository = syncJobsRepository;
    private readonly IHullerLogService _hullerLogService = hullerLogService;
    private readonly IDeltaLakeDataUploadService _deltaLakeDataUploadService = deltaLakeDataUploadService;
    private readonly ILogger<SyncHullerLogHandler> _logger = logger;

    public async Task Handle(SyncHullerLogCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.HullerLog).ConfigureAwait(false);

        var moreData = true;
        while (moreData)
        {
            _logger.LogWarning(
                "SyncHullerLogHandler: Sync job version {Version} for {JobName} started.",
                currentSyncJob.Version,
                SyncJobName.HullerLog);

            var meteringPointsToSync = _meteringPointRepository
                .GetMeteringPointsToSyncAsync(currentSyncJob.Version);

            var maxVersion = await HandleBatchAsync(meteringPointsToSync).ConfigureAwait(false);
            if (maxVersion is not null && maxVersion > DateTimeOffset.MinValue)
            {
                currentSyncJob = currentSyncJob with { Version = maxVersion.Value };
                await _syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);
            }
            else
            {
                moreData = false;
            }
        }
    }

    private async Task<DateTimeOffset?> HandleBatchAsync(IAsyncEnumerable<MeteringPoint> meteringPointsToSync)
    {
        DateTimeOffset? maxVersion = null;

        var meteringPointsToInsert = new List<HullerLogDto>();
        var meteringPointsToDelete = new List<HullerLogEmptyDto>();

        await foreach (var mp in meteringPointsToSync.ConfigureAwait(false))
        {
            maxVersion = maxVersion is null || mp.Version > maxVersion ? mp.Version : maxVersion;
            meteringPointsToDelete.Add(new HullerLogEmptyDto(mp.Identification.Value));

            foreach (var hullerLog in _hullerLogService.GetHullerLog(mp))
            {
                meteringPointsToInsert.Add(hullerLog);
            }
        }

        if (meteringPointsToDelete.Count > 0)
        {
            await _deltaLakeDataUploadService
                .DeleteHullerLogPeriodsAsync(meteringPointsToDelete)
                .ConfigureAwait(false);
        }

        if (meteringPointsToInsert.Count > 0)
        {
            await _deltaLakeDataUploadService
                .InsertHullerLogPeriodsAsync(meteringPointsToInsert)
                .ConfigureAwait(false);
        }

        return maxVersion;
    }
}
