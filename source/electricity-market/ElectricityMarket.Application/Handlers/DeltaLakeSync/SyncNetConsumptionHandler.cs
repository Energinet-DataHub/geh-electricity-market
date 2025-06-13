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

public sealed class SyncNetConsumptionHandler : IRequestHandler<SyncNetConsumptionCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly INetConsumptionService _netConsumptionService;
    private readonly IDeltaLakeDataUploadService _deltaLakeDataUploadService;
    private readonly ILogger<SyncNetConsumptionHandler> _logger;

    public SyncNetConsumptionHandler(
        IMeteringPointRepository meteringPointRepository,
        ISyncJobsRepository syncJobsRepository,
        INetConsumptionService netConsumptionService,
        IDeltaLakeDataUploadService deltaLakeDataUploadService,
        ILogger<SyncNetConsumptionHandler> logger)
    {
        _meteringPointRepository = meteringPointRepository;
        _syncJobsRepository = syncJobsRepository;
        _netConsumptionService = netConsumptionService;
        _deltaLakeDataUploadService = deltaLakeDataUploadService;
        _logger = logger;
    }

    public async Task Handle(SyncNetConsumptionCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.NetConsumption).ConfigureAwait(false);

        var moreData = true;
        while (moreData)
        {
            _logger.LogWarning(
                "SyncNetConsumptionHandler: Sync job version {Version} for {JobName} started.",
                currentSyncJob.Version,
                SyncJobName.NetConsumption);

            var meteringPointHierarchiesToSync = _meteringPointRepository
                .GetNetConsumptionMeteringPointHierarchiesToSyncAsync(currentSyncJob.Version, 10000);

            var maxVersionProcessed = await HandleBatchAsync(meteringPointHierarchiesToSync).ConfigureAwait(false);

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

    private async Task<DateTimeOffset?> HandleBatchAsync(IAsyncEnumerable<MeteringPointHierarchy> meteringPointHierarchiesToSync)
    {
        DateTimeOffset? maxVersion = null;

        var meteringPointsToDelete = new List<NetConsumptionEmptyDto>();
        var meteringPointParentsToInsert = new List<NetConsumptionParentDto>();
        var meteringPointChildrenToInsert = new List<NetConsumptionChildDto>();

        await foreach (var hierarchy in meteringPointHierarchiesToSync.ConfigureAwait(false))
        {
            maxVersion = maxVersion is null || maxVersion < hierarchy.Version ? hierarchy.Version : maxVersion;
            meteringPointsToDelete.Add(new NetConsumptionEmptyDto(hierarchy.Parent.Identification.Value));

            var netConsumptionParentDtos = _netConsumptionService.GetParentNetConsumption(hierarchy);
            if (netConsumptionParentDtos.Any())
            {
                meteringPointParentsToInsert.AddRange(netConsumptionParentDtos);

                var netConsumptionChildDtos = _netConsumptionService.GetChildNetConsumption(hierarchy).Distinct().ToList();

                if (netConsumptionChildDtos.Count != 0)
                {
                    meteringPointChildrenToInsert.AddRange(netConsumptionChildDtos);
                }
            }
        }

        if (meteringPointsToDelete.Count > 0)
        {
            await _deltaLakeDataUploadService.DeleteNetConsumptionParentsAsync(meteringPointsToDelete).ConfigureAwait(false);
            await _deltaLakeDataUploadService.DeleteNetConsumptionChildrenAsync(meteringPointsToDelete).ConfigureAwait(false);
        }

        if (meteringPointParentsToInsert.Count > 0)
        {
            await _deltaLakeDataUploadService.InsertNetConsumptionParentsAsync(meteringPointParentsToInsert).ConfigureAwait(false);
        }

        if (meteringPointChildrenToInsert.Count > 0)
        {
            await _deltaLakeDataUploadService.InsertNetConsumptionChildrenAsync(meteringPointChildrenToInsert).ConfigureAwait(false);
        }

        return maxVersion;
    }
}
