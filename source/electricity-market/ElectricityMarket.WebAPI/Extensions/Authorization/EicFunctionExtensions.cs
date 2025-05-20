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

using Energinet.DataHub.MarketParticipant.Authorization.Model;

namespace ElectricityMarket.WebAPI.Extensions.Authorization;

public static class EicFunctionExtensions
{
    public static EicFunction ToAuthorizationRole(this Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction eicFunction)
    {
        return eicFunction switch
        {
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.BalanceResponsibleParty => EicFunction.BalanceResponsibleParty,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.BillingAgent => EicFunction.BillingAgent,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.EnergySupplier => EicFunction.EnergySupplier,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.GridAccessProvider => EicFunction.GridAccessProvider,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.ImbalanceSettlementResponsible => EicFunction.ImbalanceSettlementResponsible,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.MeterOperator => EicFunction.MeterOperator,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.MeteredDataAdministrator => EicFunction.MeteredDataAdministrator,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.MeteredDataResponsible => EicFunction.MeteredDataResponsible,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.MeteringPointAdministrator => EicFunction.MeteringPointAdministrator,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.SystemOperator => EicFunction.SystemOperator,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.DanishEnergyAgency => EicFunction.DanishEnergyAgency,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.DataHubAdministrator => EicFunction.DataHubAdministrator,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.IndependentAggregator => EicFunction.IndependentAggregator,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.SerialEnergyTrader => EicFunction.SerialEnergyTrader,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.Delegated => EicFunction.Delegated,
            Energinet.DataHub.ElectricityMarket.Domain.Models.Actors.EicFunction.ItSupplier => EicFunction.ItSupplier,
            _ => throw new ArgumentOutOfRangeException(nameof(eicFunction), eicFunction, null)
        };
    }
}
