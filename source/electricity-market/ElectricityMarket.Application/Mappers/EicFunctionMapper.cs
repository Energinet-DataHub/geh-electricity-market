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

using Energinet.DataHub.ElectricityMarket.Integration.Models.Common;

namespace Energinet.DataHub.ElectricityMarket.Application.Mappers;

public static class EicFunctionMapper
{
    public static EicFunction Map(Domain.Models.Actors.EicFunction inputFunction)
    {
        return inputFunction switch
        {
            Domain.Models.Actors.EicFunction.GridAccessProvider => EicFunction.GridAccessProvider,
            Domain.Models.Actors.EicFunction.BalanceResponsibleParty => EicFunction.BalanceResponsibleParty,
            Domain.Models.Actors.EicFunction.BillingAgent => EicFunction.BillingAgent,
            Domain.Models.Actors.EicFunction.EnergySupplier => EicFunction.EnergySupplier,
            Domain.Models.Actors.EicFunction.ImbalanceSettlementResponsible => EicFunction.ImbalanceSettlementResponsible,
            Domain.Models.Actors.EicFunction.MeterOperator => EicFunction.MeterOperator,
            Domain.Models.Actors.EicFunction.MeteredDataAdministrator => EicFunction.MeteredDataAdministrator,
            Domain.Models.Actors.EicFunction.MeteredDataResponsible => EicFunction.MeteredDataResponsible,
            Domain.Models.Actors.EicFunction.MeteringPointAdministrator => EicFunction.MeteringPointAdministrator,
            Domain.Models.Actors.EicFunction.SystemOperator => EicFunction.SystemOperator,
            Domain.Models.Actors.EicFunction.DanishEnergyAgency => EicFunction.DanishEnergyAgency,
            Domain.Models.Actors.EicFunction.DataHubAdministrator => EicFunction.DataHubAdministrator,
            Domain.Models.Actors.EicFunction.IndependentAggregator => EicFunction.IndependentAggregator,
            Domain.Models.Actors.EicFunction.SerialEnergyTrader => EicFunction.SerialEnergyTrader,
            Domain.Models.Actors.EicFunction.Delegated => EicFunction.Delegated,
            Domain.Models.Actors.EicFunction.ItSupplier => EicFunction.ItSupplier,
            _ => throw new ArgumentOutOfRangeException(nameof(inputFunction), inputFunction, null),
        };
    }
}
