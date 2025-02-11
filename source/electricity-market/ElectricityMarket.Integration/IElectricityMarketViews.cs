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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Integration.Models.Common;
using Energinet.DataHub.ElectricityMarket.Integration.Models.GridAreas;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Energinet.DataHub.ElectricityMarket.Integration.Models.ProcessDelegation;
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
    Task<IEnumerable<MeteringPointMasterData>> GetMeteringPointMasterDataChangesAsync(
        MeteringPointIdentification meteringPointId,
        Interval period);

    /// <summary>
    /// Gets the master data changes in the specified period for the specified metering point.
    /// </summary>
    /// <param name="actorNumber">The GLN number of the actor to get delegations for.</param>
    /// <param name="actorRole">The EIC function of the actor.</param>
    /// <param name="gridAreaCode">The grid area code for which you want delegations.</param>
    /// <param name="processType">The type of delegation that you want.</param>
    /// <returns>If a delegations exists it is returned, otherwise null.</returns>
    Task<ProcessDelegationDto?> GetProcessDelegationAsync(
        string actorNumber,
        EicFunction actorRole,
        string gridAreaCode,
        DelegatedProcess processType);

    /// <summary>
    /// Gets the owner of a specific grid area.
    /// </summary>
    /// <param name="gridAreaCode">The grid area code for which you want the owner.</param>
    /// <returns>info about the owner of the grid area.</returns>
    Task<GridAreaOwnerDto?> GetGridAreaOwnerAsync(string gridAreaCode);
}
