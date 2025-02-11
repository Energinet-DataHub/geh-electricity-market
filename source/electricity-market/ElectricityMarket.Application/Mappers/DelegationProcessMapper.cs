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

namespace Energinet.DataHub.ElectricityMarket.Application.Mappers;

public static class DelegationProcessMapper
{
    public static Domain.Models.Common.DelegatedProcess Map(DelegatedProcess inputProcess)
    {
        return inputProcess switch
        {
            DelegatedProcess.RequestEnergyResults => Domain.Models.Common.DelegatedProcess.RequestEnergyResults,
            DelegatedProcess.ReceiveEnergyResults => Domain.Models.Common.DelegatedProcess.ReceiveEnergyResults,
            DelegatedProcess.RequestWholesaleResults => Domain.Models.Common.DelegatedProcess.RequestWholesaleResults,
            DelegatedProcess.ReceiveWholesaleResults => Domain.Models.Common.DelegatedProcess.ReceiveWholesaleResults,
            DelegatedProcess.RequestMeteringPointData => Domain.Models.Common.DelegatedProcess.RequestMeteringPointData,
            DelegatedProcess.ReceiveMeteringPointData => Domain.Models.Common.DelegatedProcess.ReceiveMeteringPointData,
            _ => throw new ArgumentOutOfRangeException(nameof(inputProcess), inputProcess, null),
        };
    }

    public static DelegatedProcess Map(Domain.Models.Common.DelegatedProcess inputProcess)
    {
        return inputProcess switch
        {
            Domain.Models.Common.DelegatedProcess.RequestEnergyResults => DelegatedProcess.RequestEnergyResults,
            Domain.Models.Common.DelegatedProcess.ReceiveEnergyResults => DelegatedProcess.ReceiveEnergyResults,
            Domain.Models.Common.DelegatedProcess.RequestWholesaleResults => DelegatedProcess.RequestWholesaleResults,
            Domain.Models.Common.DelegatedProcess.ReceiveWholesaleResults => DelegatedProcess.ReceiveWholesaleResults,
            Domain.Models.Common.DelegatedProcess.RequestMeteringPointData => DelegatedProcess.RequestMeteringPointData,
            Domain.Models.Common.DelegatedProcess.ReceiveMeteringPointData => DelegatedProcess.ReceiveMeteringPointData,
            _ => throw new ArgumentOutOfRangeException(nameof(inputProcess), inputProcess, null),
        };
    }
}
