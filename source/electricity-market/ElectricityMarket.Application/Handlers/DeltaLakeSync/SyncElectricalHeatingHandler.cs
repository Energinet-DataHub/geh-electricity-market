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

public sealed class SyncElectricalHeatingHandler : IRequestHandler<SyncElectricalHeatingCommand>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly ISyncJobsRepository _syncJobsRepository;
    private readonly IElectricalHeatingPeriodizationService _electricalHeatingPeriodizationService;
    private readonly IDeltaLakeDataUploadService _deltaLakeDataUploadService;
    private readonly ILogger<SyncElectricalHeatingHandler> _logger;
    private readonly List<int?> _relevantSettlementGroups = [null, 2, 6];

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
                .GetElectricalHeatingMeteringPointHierarchiesToSyncAsync(currentSyncJob);

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

    private async Task<DateTimeOffset> HandleBatchAsync(IAsyncEnumerable<MeteringPointHierarchy> meteringPointHierarchies)
    {
        var maxVersion = DateTimeOffset.MinValue;
        var parentMeteringPointsToInsert = new List<ElectricalHeatingParentDto>();
        var childMeteringPointsToInsert = new List<ElectricalHeatingChildDto>();
        var parentMeteringPointsToDelete = new List<ElectricalHeatingEmptyDto>();
        var childMeteringPointsToDelete = new List<ElectricalHeatingEmptyDto>();
        await foreach (var hierarchy in meteringPointHierarchies.ConfigureAwait(false))
        {
            maxVersion = hierarchy.Version > maxVersion ? hierarchy.Version : maxVersion;
            parentMeteringPointsToInsert.AddRange(_electricalHeatingPeriodizationService.GetParentElectricalHeating(hierarchy.Parent));

            var parentSettlementGroups = hierarchy.Parent.MetadataTimeline.Select(m => m.NetSettlementGroup).Distinct().ToList();
            if (parentSettlementGroups.Intersect(_relevantSettlementGroups).Any())
            {
                childMeteringPointsToInsert.AddRange(
                    _electricalHeatingPeriodizationService.GetChildElectricalHeating(hierarchy.ChildMeteringPoints));
            }

            parentMeteringPointsToDelete.Add(new ElectricalHeatingEmptyDto(hierarchy.Parent.Identification.Value));
            childMeteringPointsToDelete.AddRange(hierarchy.ChildMeteringPoints.Select(cmp => new ElectricalHeatingEmptyDto(cmp.Identification.Value)));
        }

        if (parentMeteringPointsToDelete.Count > 0)
        {
            await _deltaLakeDataUploadService.DeleteParentElectricalHeatingAsync(parentMeteringPointsToDelete)
                .ConfigureAwait(false);
        }

        if (childMeteringPointsToDelete.Count > 0)
        {
            await _deltaLakeDataUploadService.DeleteChildElectricalHeatingAsync(childMeteringPointsToDelete)
                .ConfigureAwait(false);
        }

        if (parentMeteringPointsToInsert.Count > 0)
        {
            await _deltaLakeDataUploadService.InsertParentElectricalHeatingAsync(parentMeteringPointsToInsert)
                .ConfigureAwait(false);
        }

        if (childMeteringPointsToInsert.Count > 0)
        {
            await _deltaLakeDataUploadService.InsertChildElectricalHeatingAsync(childMeteringPointsToInsert).ConfigureAwait(false);
        }

        return maxVersion;
    }
}
