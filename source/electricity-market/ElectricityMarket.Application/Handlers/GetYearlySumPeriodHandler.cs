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
        {
            return null;
        }

        if (meteringPoint.EnergySupplyPeriod != null)
        {
            var startOfsupplyLessThan365Go = meteringPoint.EnergySupplyPeriod.Valid.Start.CompareTo(Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime().AddDays(-365))) > 0;
            if (startOfsupplyLessThan365Go)
            {
                if (meteringPoint.EnergySupplyPeriod.Valid.End.CompareTo(Instant.FromDateTimeOffset(DateTime.MaxValue)) == 0)
                {
                    // Start from current supply start up till today.
                    return new Interval(meteringPoint.EnergySupplyPeriod.Valid.Start, Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime()));
                }

                // Start and end equal to actual supply period.
                return new Interval(meteringPoint.EnergySupplyPeriod.Valid.Start, meteringPoint.EnergySupplyPeriod.Valid.End);
            }

            // Start 365 days ago, end Today or equal to supplier end when before today.
            return new Interval(Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime().AddDays(-365)), meteringPoint.EnergySupplyPeriod.Valid.End.CompareTo(Instant.FromDateTimeOffset(DateTime.MaxValue)) == 0 ? Instant.FromDateTimeUtc(DateTime.Today.ToUniversalTime()) : meteringPoint.EnergySupplyPeriod.Valid.End);
        }

        // No supplier return null.
        return null;
    }

}
