﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Collections.Generic;
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Integration;

public interface IElectricityMarketViews
{
    /// <summary>
    /// Gets the master data changes in the specified period for the specified metering point.
    /// </summary>
    /// <param name="meteringPointId">The identifier of the metering point.</param>
    /// <param name="period">The period in which to look up master data changes for the given metering point.</param>
    /// <returns>The list of metering point master data changes within the specified period.</returns>
    IAsyncEnumerable<MeteringPointMasterData> GetMeteringPointMasterDataChangesAsync(
        MeteringPointIdentification meteringPointId,
        Interval period);

    /// <summary>
    /// Gets the energy suppliers for the specified metering point in the specified period.
    /// </summary>
    /// <param name="meteringPointId">The identifier of the metering point.</param>
    /// <param name="period">The period in which to look up energy suppliers for the given metering point.</param>
    /// <returns>The energy supplier for the metering point at the specified date; or NULL.</returns>
    IAsyncEnumerable<MeteringPointEnergySupplier> GetMeteringPointEnergySuppliersAsync(
        MeteringPointIdentification meteringPointId,
        Interval period);
}
