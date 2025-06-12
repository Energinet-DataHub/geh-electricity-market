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

public sealed class SyncMeasurementsReportHandler(
    IMeteringPointRepository meteringPointRepository,
    ISyncJobsRepository syncJobsRepository,
    IMeasurementsReportService measurementsReportService,
    IDeltaLakeDataUploadService deltaLakeDataUploadService,
    ILogger<SyncMeasurementsReportHandler> logger) : IRequestHandler<SyncMeasurementsReportCommand>
{
    public async Task Handle(SyncMeasurementsReportCommand request, CancellationToken cancellationToken)
    {
        var currentSyncJob = await syncJobsRepository.GetByNameAsync(SyncJobName.MeasurementsReport).ConfigureAwait(false);

        var moreData = true;
        while (moreData)
        {
            logger.LogWarning(
                "SyncMeasurementsReportHandler: Sync job version {Version} for {JobName} started.",
                currentSyncJob.Version,
                SyncJobName.MeasurementsReport);

            var meteringPointsToSync = meteringPointRepository
                .GetMeteringPointHierarchiesToSyncAsync(currentSyncJob);

            var maxVersion = await HandleBatchAsync(meteringPointsToSync).ConfigureAwait(false);
            if (maxVersion is not null && maxVersion > DateTimeOffset.MinValue)
            {
                currentSyncJob = currentSyncJob with { Version = maxVersion.Value };
                await syncJobsRepository.AddOrUpdateAsync(currentSyncJob).ConfigureAwait(false);
            }
            else
            {
                moreData = false;
            }
        }
    }

    private async Task<DateTimeOffset?> HandleBatchAsync(IAsyncEnumerable<MeteringPointHierarchy> meteringPointsToSync)
    {
        DateTimeOffset? maxVersion = null;

        var meteringPointsToInsert = new List<MeasurementsReportDto>();
        var meteringPointsToDelete = new List<MeasurementsReportEmptyDto>();

        await foreach (var mph in meteringPointsToSync.ConfigureAwait(false))
        {
            maxVersion = maxVersion is null || mph.Version > maxVersion ? mph.Version : maxVersion;
            meteringPointsToDelete.Add(new MeasurementsReportEmptyDto(mph.Parent.Identification.Value));

            foreach (var measurementsReport in measurementsReportService.GetMeasurementsReport(mph))
            {
                meteringPointsToInsert.Add(measurementsReport);
            }
        }

        if (meteringPointsToDelete.Count > 0)
        {
            await deltaLakeDataUploadService
                .DeleteMeasurementsReportPeriodsAsync(meteringPointsToDelete)
                .ConfigureAwait(false);
        }

        if (meteringPointsToInsert.Count > 0)
        {
            await deltaLakeDataUploadService
                .InsertMeasurementsReportPeriodsAsync(meteringPointsToInsert)
                .ConfigureAwait(false);
        }

        return maxVersion;
    }
}
