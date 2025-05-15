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

using Energinet.DataHub.ElectricityMarket.Application.Helpers;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class HullerLogService(IMeteringPointRepository meteringPointRepository) : IHullerLogService
{
    private readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
    private readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.SupplyToGrid, MeteringPointType.ConsumptionFromGrid, MeteringPointType.ElectricalHeating, MeteringPointType.NetConsumption];
    private readonly Instant _cutoffDate = InstantPattern.ExtendedIso.Parse("2021-01-01T00:00:00Z").Value;

    private readonly IMeteringPointRepository _meteringPointRepository = meteringPointRepository;

    public Task<IReadOnlyList<HullerLogDto>> GetHullerLogAsync(MeteringPoint meteringPoint)
    {
    }
}
