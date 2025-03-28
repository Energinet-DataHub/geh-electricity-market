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
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;

namespace Energinet.DataHub.ElectricityMarket.Application.Services.DeltaLakeSync;

public class ElectricalHeatingSyncService : IElectricalHeatingSyncService
{
    private readonly IMeteringPointRepository _meteringPointRepository;

    public ElectricalHeatingSyncService(IMeteringPointRepository meteringPointRepository)
    {
        _meteringPointRepository = meteringPointRepository;
    }

    public async ValueTask<bool> SyncElectricalHeatingAsync()
    {
        var meteringPointsToSync = await _meteringPointRepository
            .GetMeteringPointsToSyncAsync(0)
            .ConfigureAwait(false);

        return meteringPointsToSync.Count() == 1;
    }
}
