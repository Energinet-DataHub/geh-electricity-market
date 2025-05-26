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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Authorize;
using Energinet.DataHub.ElectricityMarket.Application.Commands.GridArea;
using Energinet.DataHub.ElectricityMarket.Application.Handlers;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.GridAreas;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Handlers;

[UnitTest]
public sealed class VerifyGridOwnerHandlerTests
{
    [Fact]
    public async Task Handle_GridAreaNotEqual_ReturnsFalse()
    {
        // TBC!

        // Arrange
        var gridAreaRepository = new Mock<IMeteringPointRepository>();
        var target = new VerifyGridOwnerHandler(gridAreaRepository.Object);

        /*
        gridAreaRepository
            .Setup(repo => repo.(It.IsAny<string>()))
            .ReturnsAsync((GridAreaOwnerDto?)null);
        */

        var gridAreaCode = new List<string>
        {
            "12345",
            "6789"
        };
        var command = new VerifyGridOwnerCommand(
                MeteringPointIdentification: "123456789012345678",
                GridAreaCodes: gridAreaCode.AsReadOnly());

        // Act + Assert
        Assert.False(await target.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FindGridAreaOwner_ReturnsSuccesful()
    {
        // Arrange
        var gridAreaRepository = new Mock<IGridAreaRepository>();
        var target = new GetGridAreaOwnerHandler(gridAreaRepository.Object);

        var gridArea = new GridAreaOwnerDto("1234");

        gridAreaRepository
            .Setup(repo => repo.GetGridAreaOwnerAsync("111"))
            .ReturnsAsync(gridArea);

        var command = new GetGridAreaOwnerCommand("111");

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.NotNull(response);
        Assert.Equal("1234", response.GridAccessProviderGln);
    }
}
