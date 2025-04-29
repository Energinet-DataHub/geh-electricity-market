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
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers.DeltaLakeSync;

public sealed class SyncElectricalHeatingHandler : IRequestHandler<SyncElectricalHeatingCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly IDeltaLakeDataUploadService _deltaLakeDataUploadService;

    public SyncElectricalHeatingHandler(IMeteringPointRepository meteringPointRepository, ISyncJobsRepository syncJobsRepository, IDeltaLakeDataUploadService deltaLakeDataUploadService)
    {
        _meteringPointRepository = meteringPointRepository;
        _syncJobsRepository = syncJobsRepository;
        _deltaLakeDataUploadService = deltaLakeDataUploadService;
    }

    public async Task Handle(SyncElectricalHeatingCommand request, CancellationToken cancellationToken)
    {
        //var currentSyncJob = await _syncJobsRepository.GetByNameAsync(SyncJobName.ElectricalHeating).ConfigureAwait(false);
        //var meteringPointsToSync = _meteringPointRepository
        //    .GetMeteringPointsToSyncAsync(currentSyncJob.Version)
        //    .ConfigureAwait(false);

        //DateTimeOffset maxVersion = currentSyncJob.Version;
        //await foreach (var meteringPoint in meteringPointsToSync)
        //{
        //    // TODO: Implement the sync logic to Databricks for electrical heating metering points
        //    maxVersion = meteringPoint.Version > maxVersion ? meteringPoint.Version : maxVersion;
        //}

        //currentSyncJob = currentSyncJob with { Version = maxVersion };
        //await _syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);

        var mps = new List<ElectricalHeatingChildDto>
        {
            new ElectricalHeatingChildDto("570714700000002601", "Consumption", "Physical", "570714700000004704", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1)),
            new ElectricalHeatingChildDto("570714700000002602", "Consumption", "Physical", "570714700000004703", DateTimeOffset.Now.AddDays(-2), DateTimeOffset.Now.AddDays(2))
        };
        await _deltaLakeDataUploadService.ImportTransactionsAsync(mps).ConfigureAwait(false);
    }
}
