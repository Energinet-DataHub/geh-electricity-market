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
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetRelatedMeteringPointsHandler : IRequestHandler<GetRelatedMeteringPointsCommand, GetRelatedMeteringPointsResponse?>
{
    private readonly IMeteringPointRepository _meteringPointRepository;

    public GetRelatedMeteringPointsHandler(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task<GetRelatedMeteringPointsResponse?> Handle(GetRelatedMeteringPointsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var meteringPoint = await _meteringPointRepository
            .GetAsync(new MeteringPointIdentification(request.Identification))
            .ConfigureAwait(false);

        if (meteringPoint == null)
        {
            return null;
        }

        IEnumerable<MeteringPoint>? relatedMeteringPoints;
        RelatedMeteringPointDto? parent = null;
        if (meteringPoint.Metadata.Parent is not null)
        {
            relatedMeteringPoints = await _meteringPointRepository
                .GetRelatedMeteringPointsAsync(meteringPoint.Metadata.Parent)
                .ConfigureAwait(false);
            parent = MapToRelated(await _meteringPointRepository.GetAsync(meteringPoint.Metadata.Parent).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("Parent metering point not found even though it should have one"));
        }
        else
        {
            relatedMeteringPoints = await _meteringPointRepository
                .GetRelatedMeteringPointsAsync(new MeteringPointIdentification(request.Identification))
                .ConfigureAwait(false);
        }

        relatedMeteringPoints = relatedMeteringPoints?.Where(x => x.Identification != meteringPoint.Identification).ToList();

        var relatedPoints = relatedMeteringPoints?
            .Where(x => x.Metadata.Parent == meteringPoint.Identification
                        && x.Metadata.Valid.End.ToDateTimeOffset() > DateTimeOffset.Now)
            .OrderBy(y => y.Metadata.Type)
            .Select(MapToRelated) ?? [];

        var relatedByGsrn = relatedMeteringPoints?
            .Where(x => string.IsNullOrEmpty(x.Metadata?.Parent?.Value)
                        && !string.IsNullOrWhiteSpace(x.Metadata?.PowerPlantGsrn)
                        && x.Metadata.PowerPlantGsrn == meteringPoint.Metadata.PowerPlantGsrn)
            .OrderBy(y => y.Metadata.Type)
            .Select(MapToRelated) ?? [];

        var historical = relatedMeteringPoints?
            .Where(x => x.MetadataTimeline.Any(
                            y => y.Parent == meteringPoint.Identification
                                && y.Valid.End.ToDateTimeOffset() < DateTimeOffset.Now)
                                && string.IsNullOrWhiteSpace(x.Metadata.PowerPlantGsrn))
            .OrderBy(y => y.Metadata.Type)
            .Select(MapToRelated) ?? [];

        var historicalByGsrn = relatedMeteringPoints?
            .Where(x => x.MetadataTimeline.Any(
                            y => y.Parent == meteringPoint.Identification
                                 && y.Valid.End.ToDateTimeOffset() < DateTimeOffset.Now)
                        && !string.IsNullOrWhiteSpace(x.Metadata.PowerPlantGsrn)
                        && x.Metadata.PowerPlantGsrn == meteringPoint.Metadata.PowerPlantGsrn)
            .OrderBy(y => y.Metadata.Type)
            .Select(MapToRelated) ?? [];

        return new GetRelatedMeteringPointsResponse(
            new RelatedMeteringPointsDto(
                MapToRelated(meteringPoint),
                parent,
                relatedPoints,
                relatedByGsrn,
                historical,
                historicalByGsrn));
    }

    private static RelatedMeteringPointDto MapToRelated(MeteringPoint meteringPoint)
    {
        return new RelatedMeteringPointDto(
            meteringPoint.Id,
            meteringPoint.Identification.Value,
            meteringPoint.Metadata.Type,
            meteringPoint.Metadata.ConnectionState,
            FindFirstConnectedDate(meteringPoint.MetadataTimeline),
            FindClosedDownDate(meteringPoint.MetadataTimeline));
    }

    private static DateTimeOffset? FindFirstConnectedDate(IEnumerable<MeteringPointMetadata> meteringPointPeriods)
    {
        return meteringPointPeriods
            .Where(mp => mp.ConnectionState == ConnectionState.Connected)
            .OrderBy(mp => mp.Valid.Start)
            .Select(mp => mp.Valid.Start.ToDateTimeOffset())
            .FirstOrDefault();
    }

    private static DateTimeOffset? FindClosedDownDate(IEnumerable<MeteringPointMetadata> meteringPointPeriods)
    {
        return meteringPointPeriods
            .Where(mp => mp.ConnectionState == ConnectionState.ClosedDown)
            .OrderByDescending(mp => mp.Valid.Start)
            .Select(mp => mp.Valid.Start.ToDateTimeOffset())
            .FirstOrDefault();
    }
}
