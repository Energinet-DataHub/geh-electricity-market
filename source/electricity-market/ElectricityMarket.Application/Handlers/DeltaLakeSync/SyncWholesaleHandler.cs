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

// TODO: WORK IN PROGRESS - Revisit this task in Phase 3
public sealed class SyncWholesaleHandler : IRequestHandler<SyncWholesaleCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly IWholesaleService _wholesaleService;

    public SyncWholesaleHandler(
        IMeteringPointRepository meteringPointRepository,
        ISyncJobsRepository syncJobsRepository,
        IWholesaleService wholesaleService)
    {
        _meteringPointRepository = meteringPointRepository;
        _syncJobsRepository = syncJobsRepository;
        _wholesaleService = wholesaleService;
    }

    public async Task Handle(SyncWholesaleCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.Wholesale).ConfigureAwait(false);
        var meteringPointsToSync = _meteringPointRepository
            .GetMeteringPointsToSyncAsync(currentSyncJob.Version);

        while (await meteringPointsToSync.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var maxVersion = await meteringPointsToSync.MaxAsync(mp => mp.Version, cancellationToken).ConfigureAwait(false);

            await HandleBatchAsync(meteringPointsToSync, maxVersion, cancellationToken).ConfigureAwait(false);
            currentSyncJob = currentSyncJob with { Version = maxVersion };
            await _syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);
            meteringPointsToSync = _meteringPointRepository.GetMeteringPointsToSyncAsync(currentSyncJob.Version);
        }
    }

    private async Task HandleBatchAsync(IAsyncEnumerable<MeteringPoint> meteringPointsToSync, DateTimeOffset maxVersion, CancellationToken cancellationToken)
    {
        var wholesaleMeteringPoints = await _wholesaleService.GetWholesaleMeteringPointsAsync(meteringPointsToSync).ConfigureAwait(false);

        await foreach (var meteringPoint in meteringPointsToSync)
        {
            // TODO: Implement the sync logic to Databricks for net consumption metering points.
        }
    }
}
