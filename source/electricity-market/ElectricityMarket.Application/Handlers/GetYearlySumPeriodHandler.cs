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

using Azure.Core;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Authorize;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetYearlySumPeriodHandler : IRequestHandler<GetYearlySumPeriodCommand, Interval?>
{
    private readonly IMeteringPointRepository _meteringPointRepository;
    private readonly Instant _yearAgo = Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime().AddDays(-365));
    private readonly Instant _today = Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime());

    public GetYearlySumPeriodHandler(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task<Interval?> Handle(GetYearlySumPeriodCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var meteringPoint = await _meteringPointRepository.GetMeteringPointForSignatureAsync(
           new MeteringPointIdentification(request.MeteringPointIdentification))
            .ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(meteringPoint, nameof(meteringPoint));

        // Only possible for E17/E18 metering point types.
        if (meteringPoint.Metadata.Type != MeteringPointType.Consumption && meteringPoint.Metadata.Type != MeteringPointType.Production)
            return null;

        if (meteringPoint.EnergySupplyPeriod != null)
        {
            var startOfsupplyLessThan365Go = meteringPoint.EnergySupplyPeriod.Valid.Start.CompareTo(_yearAgo) > 0;
            if (startOfsupplyLessThan365Go)
            {
                if (meteringPoint.EnergySupplyPeriod.Valid.End.CompareTo(Instant.FromDateTimeOffset(DateTime.MaxValue)) >= 0)
                {
                    // Start from current supply start up till today.
                    return new Interval(meteringPoint.EnergySupplyPeriod.Valid.Start, _today);
                }

                // Start and end equal to actual supply period.
                return new Interval(meteringPoint.EnergySupplyPeriod.Valid.Start, meteringPoint.EnergySupplyPeriod.Valid.End);
            }

            // Start 365 days ago, end Today or equal to supplier end when before today.
            return new Interval(_yearAgo, meteringPoint.EnergySupplyPeriod.Valid.End.CompareTo(Instant.FromDateTimeOffset(DateTime.MaxValue)) >= 0 ? _today : meteringPoint.EnergySupplyPeriod.Valid.End);
        }

        // Retrieve commercial relations with end date in the last year (yearAgo).
        var filteredCommercialRelations = meteringPoint.CommercialRelationTimeline.Where(c => (c.Period.End >= _yearAgo) && (c.Period.End <= _today));

        // No supplier in the last year: return null.
        if (filteredCommercialRelations == null)
            return null;

        var mostRecentMovein = filteredCommercialRelations.OrderByDescending(x => x.Period.Start).FirstOrDefault()?
                .EnergySupplyPeriodTimeline.OrderByDescending(x => x.Valid.Start).First();

        // Null when no result, less than year ago => start date else year ago date.
        return mostRecentMovein == null ? null : new Interval(mostRecentMovein.Valid.Start.CompareTo(_yearAgo) > 0 ? mostRecentMovein.Valid.Start : _yearAgo, mostRecentMovein.Valid.End);

    }
}
