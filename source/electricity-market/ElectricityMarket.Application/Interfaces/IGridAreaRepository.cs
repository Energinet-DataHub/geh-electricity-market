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
using Energinet.DataHub.ElectricityMarket.Integration.Models.ProcessDelegation;

<<<<<<<< HEAD:source/electricity-market/ElectricityMarket.Application/Interfaces/IGridAreaRepository.cs
using Energinet.DataHub.ElectricityMarket.Integration.Models.GridAreas;

namespace Energinet.DataHub.ElectricityMarket.Application.Interfaces;

public interface IGridAreaRepository
{
    Task<GridAreaOwnerDto?> GetGridAreaOwnerAsync(string gridAreaCode);
========
namespace Energinet.DataHub.ElectricityMarket.Application.Interfaces;

public interface IProcessDelegationRepository
{
    Task<ProcessDelegationDto?> GetProcessDelegationAsync(ProcessDelegationRequestDto processDelegationRequest);
>>>>>>>> main:source/electricity-market/ElectricityMarket.Application/Interfaces/IProcessDelegationRepository.cs
}
