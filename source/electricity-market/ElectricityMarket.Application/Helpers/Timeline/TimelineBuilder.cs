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

using Energinet.DataHub.ElectricityMarket.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Application.Helpers.Timeline
{
    public class TimelineBuilder
    {
        private readonly List<TimelineSegment> _segments = [];

        public void AddSegment(
            MeteringPointIdentification? identification,
            Instant start,
            MeteringPointMetadata metadata,
            CommercialRelation? commercialRelation)
        {
            if (_segments.Count > 0)
            {
                var prev = _segments[^1];
                _segments[^1] = prev with
                {
                    Period = new Interval(prev.Period.Start, start)
                };
            }

            _segments.Add(new TimelineSegment(
                Identification: identification,
                Period: new Interval(start, Instant.MaxValue),
                Metadata: metadata,
                Relation: commercialRelation));
        }

        public IReadOnlyList<TimelineSegment> Build() => _segments;
    }
}
