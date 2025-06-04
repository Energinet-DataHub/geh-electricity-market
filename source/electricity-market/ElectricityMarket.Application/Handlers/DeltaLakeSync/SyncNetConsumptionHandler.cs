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
                .GetNetConsumptionMeteringPointHierarchiesToSyncAsync(currentSyncJob.Version);

            var maxVersion = await HandleBatchAsync(meteringPointHierarchiesToSync).ConfigureAwait(false);

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

    private async Task<DateTimeOffset> HandleBatchAsync(IAsyncEnumerable<MeteringPointHierarchy> meteringPointHierarchiesToSync)
    {
        var maxVersion = DateTimeOffset.MinValue;
        await foreach (var hierarchy in meteringPointHierarchiesToSync.ConfigureAwait(false))
        {
            maxVersion = hierarchy.Version > maxVersion ? hierarchy.Version : maxVersion;

            var netConsumptionParentDtos = _netConsumptionService.GetParentNetConsumption(hierarchy);
            if (netConsumptionParentDtos.Any())
            {
                await _deltaLakeDataUploadService.ImportTransactionsAsync(netConsumptionParentDtos).ConfigureAwait(false);

                var netConsumptionChildDtos = _netConsumptionService.GetChildNetConsumption(hierarchy).Distinct().ToList();

                if (netConsumptionChildDtos.Count != 0)
                {
                    await _deltaLakeDataUploadService.ImportTransactionsAsync(netConsumptionChildDtos).ConfigureAwait(false);
                }
            }
        }

        return maxVersion;
    }
}
