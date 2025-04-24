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

public sealed class SyncWholesaleMeteringPointHandler : IRequestHandler<SyncWholesaleMeteringPointCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly IWholesaleMeteringPointService _wholesaleMeteringPointService;

    public SyncWholesaleMeteringPointHandler(
        IMeteringPointRepository meteringPointRepository,
        ISyncJobsRepository syncJobsRepository,
        IWholesaleMeteringPointService wholesaleMeteringPointService)
    {
        _meteringPointRepository = meteringPointRepository;
        _syncJobsRepository = syncJobsRepository;
        _wholesaleMeteringPointService = wholesaleMeteringPointService;
    }

    public async Task Handle(SyncWholesaleMeteringPointCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.WholesaleMeteringPoint).ConfigureAwait(false);
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
        var wholesaleMeteringPoints = await _wholesaleMeteringPointService.GetWholesaleMeteringPointsAsync(meteringPointsToSync).ConfigureAwait(false);

        await foreach (var meteringPoint in meteringPointsToSync)
        {
            // TODO: Implement the sync logic to Databricks for wholesale metering points.
        }
    }
}
