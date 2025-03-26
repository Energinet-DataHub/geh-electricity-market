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

using Energinet.DataHub.ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Application.Mappers;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetChildAndRelatedMeteringPointsHandler : IRequestHandler<GetChildAndRelatedMeteringPointsCommand, GetChildAndRelatedMeteringPointsResponse?>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly IRoleFiltrationService _roleFiltrationService;

    public GetChildAndRelatedMeteringPointsHandler(IMeteringPointRepository meteringPointRepository, IRoleFiltrationService roleFiltrationService)
    {
        _meteringPointRepository = meteringPointRepository;
        _roleFiltrationService = roleFiltrationService;
    }

    public async Task<GetChildAndRelatedMeteringPointsResponse?> Handle(GetChildAndRelatedMeteringPointsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var meteringPoint = await _meteringPointRepository
            .GetAsync(new MeteringPointIdentification(request.Identification))
            .ConfigureAwait(false);

        var related = await _meteringPointRepository
            .GetRelatedMeteringPointsAsync(new MeteringPointIdentification(request.Identification))
            .ConfigureAwait(false);

        return meteringPoint == null
            ? null
            : new GetChildAndRelatedMeteringPointsResponse(
            new ParentWithRelatedMeteringPointDto(
                MapToRelated(meteringPoint),
                related?.Where(x => x.Metadata.Parent == meteringPoint.Identification).Select(MapToRelated) ?? [],
                related?.Where(x => x.MetadataTimeline.Any(y => y.Parent == meteringPoint.Identification)).Select(MapToRelated) ?? []));
    }

    private static RelatedMeteringPointDto MapToRelated(MeteringPoint meteringPoint)
    {
        return new RelatedMeteringPointDto(
            meteringPoint.Id,
            meteringPoint.Identification.Value,
            meteringPoint.Metadata.Type,
            meteringPoint.Metadata.ConnectionState,
            FindFirstConnectedDate(meteringPoint.MetadataTimeline),
            FindDisconnectedDate(meteringPoint.MetadataTimeline));
    }

    private static DateTimeOffset? FindFirstConnectedDate(IEnumerable<MeteringPointMetadata> meteringPointPeriods)
    {
        return meteringPointPeriods
            .Where(mp => mp.ConnectionState == ConnectionState.Connected)
            .OrderBy(mp => mp.Valid.Start)
            .Select(mp => mp.Valid.Start.ToDateTimeOffset())
            .FirstOrDefault();
    }

    private static DateTimeOffset? FindDisconnectedDate(IEnumerable<MeteringPointMetadata> meteringPointPeriods)
    {
        return meteringPointPeriods
            .Where(mp => mp.ConnectionState == ConnectionState.Disconnected)
            .OrderByDescending(mp => mp.Valid.Start)
            .Select(mp => mp.Valid.Start.ToDateTimeOffset())
            .FirstOrDefault();
    }
}
