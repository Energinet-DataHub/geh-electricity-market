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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Commands.ProcessDelegations;
using Energinet.DataHub.ElectricityMarket.Application.Handlers;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Mappers;
using Energinet.DataHub.ElectricityMarket.Integration.Models.ProcessDelegation;
using Moq;
using Xunit;
using Xunit.Categories;
using DelegatedProcess = Energinet.DataHub.ElectricityMarket.Domain.Models.Common.DelegatedProcess;
using EicFunction = Energinet.DataHub.ElectricityMarket.Integration.Models.Common.EicFunction;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Handlers;

[UnitTest]
public sealed class GetProcessDelegationHandlerTests
{
    [Fact]
    public async Task Handle_NoProcessDelegationFound_ReturnsNull()
    {
        // Arrange
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        processDelegationRepository
            .Setup(repo => repo.GetProcessDelegationAsync(It.IsAny<ProcessDelegationRequestDto>()))
            .ReturnsAsync((ProcessDelegationDto?)null);

        var request = new ProcessDelegationRequestDto("123", EicFunction.GridAccessProvider, "111", DelegationProcessMapper.Map(DelegatedProcess.ReceiveEnergyResults));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(processDelegationRepository.Object);

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.Null(response);
    }

    [Fact]
    public async Task Handle_FoundDelegations_ReturnsSuccesful()
    {
        // Arrange
        const string delegatedFromActorNumber = "111";
        const string delegatedToActorNumber = "123";
        var processDelegation = new ProcessDelegationDto(
            delegatedToActorNumber,
            EicFunction.GridAccessProvider);
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        processDelegationRepository
            .Setup(repo => repo.GetProcessDelegationAsync(It.IsAny<ProcessDelegationRequestDto>()))
            .ReturnsAsync(processDelegation);

        var request = new ProcessDelegationRequestDto(delegatedFromActorNumber, EicFunction.GridAccessProvider, "111", DelegationProcessMapper.Map(DelegatedProcess.ReceiveEnergyResults));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(processDelegationRepository.Object);

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.NotNull(response);
        Assert.Equal(delegatedToActorNumber, response.ActorNumber);
        Assert.Equal(EicFunction.GridAccessProvider.ToString(),  response.ActorRole.ToString());
    }
}
