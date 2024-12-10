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

using System.Threading.Tasks;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Integration;

public interface IElectricityMarketViews
{
    /// <summary>
    /// Gets the master data snapshot of the specified metering point at the specified date.
    /// Returns NULL if:
    /// - the metering point is missing,
    /// - the metering point has no grid access provider,
    /// - the metering point has no data at the specified date.
    /// </summary>
    /// <param name="meteringPointId">The identifier of the metering point.</param>
    /// <param name="validAt">The point in time at which to look up the master data.</param>
    /// <returns>The snapshot of master data for the metering point at the specified date; or NULL.</returns>
    Task<MeteringPointMasterData?> GetMeteringPointMasterDataAsync(
        MeteringPointIdentification meteringPointId,
        Instant validAt);

    /// <summary>
    /// Gets the energy supplier for the specified metering point at the specified date.
    /// Returns NULL if:
    /// - the metering point is missing,
    /// - the metering point has no energy supplier at the specified date.
    /// </summary>
    /// <param name="meteringPointId">The identifier of the metering point.</param>
    /// <param name="validAt">The point in time at which look up the energy supplier.</param>
    /// <returns>The energy supplier for the metering point at the specified date; or NULL.</returns>
    Task<MeteringPointEnergySupplier?> GetMeteringPointEnergySupplierAsync(
        MeteringPointIdentification meteringPointId,
        Instant validAt);
}
