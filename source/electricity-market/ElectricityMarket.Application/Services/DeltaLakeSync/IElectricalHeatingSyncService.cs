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

namespace Energinet.DataHub.ElectricityMarket.Application.Services.DeltaLakeSync;

/// <summary>
/// This service handles syncing of electrical heating data to the databricks delta lake.
/// </summary>
public interface IElectricalHeatingSyncService
{
    /// <summary>
    /// Syncs the updated electrical heating data to the databricks delta lake.
    /// </summary>
    /// <returns>True if successful, otherwise false.</returns>
    ValueTask<bool> SyncElectricalHeatingAsync();
}
