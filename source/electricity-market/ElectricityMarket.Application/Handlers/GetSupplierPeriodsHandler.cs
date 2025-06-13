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

using Energinet.DataHub.ElectricityMarket.Application.Commands.Authorize;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using MediatR;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class GetSupplierPeriodsHandler : IRequestHandler<GetSupplierPeriodsCommand, IEnumerable<Interval>>
{
    private readonly IMeteringPointRepository _meteringPointRepository;

    public GetSupplierPeriodsHandler(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async Task<IEnumerable<Interval>> Handle(GetSupplierPeriodsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var meteringPoint = await _meteringPointRepository.GetMeteringPointForSignatureAsync(
           new MeteringPointIdentification(request.MeteringPointIdentification))
            .ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(meteringPoint, nameof(meteringPoint));

        var commercialRelations = meteringPoint.CommercialRelationTimeline;
        var energySupplier = request.ActorNumber;
        var resultPeriods = new List<Interval>();
        if (commercialRelations != null)
        {
            // Retrieve commercial relations where energySupplier is energySupplier from the request and requested period overlaps the period of the Commercial relation.
            var filteredCommercialRelations = commercialRelations.Where(c => (c.EnergySupplier == energySupplier) && (c.Period.Start < request.RequestedPeriod.End)
                && (c.Period.End > request.RequestedPeriod.Start));
            if (filteredCommercialRelations == null)
            {
                return Enumerable.Empty<Interval>();
            }

            foreach (var relation in filteredCommercialRelations)
            {
                // Select the part of the commercial relation period that overlaps with the period from the request.
                var start = relation.Period.Start;
                var end = relation.Period.End;
                if (relation.Period.Start < request.RequestedPeriod.Start)
                {
                    start = request.RequestedPeriod.Start;
                }

                if (!relation.Period.HasEnd || relation.Period.End > request.RequestedPeriod.End)
                {
                    end = request.RequestedPeriod.End;
                }

                resultPeriods.Add(new Interval(start, end));
            }
        }

        return resultPeriods;
    }
}
