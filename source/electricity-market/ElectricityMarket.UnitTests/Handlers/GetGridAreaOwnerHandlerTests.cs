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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Commands.GridArea;
using Energinet.DataHub.ElectricityMarket.Application.Handlers;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Common;
using Energinet.DataHub.ElectricityMarket.Domain.Models.GridAreas;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.UnitTests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Handlers;

[UnitTest]
public sealed class GetGridAreaOwnerHandlerTests
{
    [Fact]
    public async Task Handle_GridAreaNotFound_ReturnsNull()
    {
        // Arrange
        var gridAreaRepository = new Mock<IGridAreaRepository>();
        var actorRepository = new Mock<IActorRepository>();
        var target = new GetGridAreaOwnerHandler(gridAreaRepository.Object, actorRepository.Object);

        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode(It.IsAny<string>())))
            .ReturnsAsync((GridArea?)null);

        var command = new GetGridAreaOwnerCommand("XXXXXX");

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.Null(response);
    }

    [Fact]
    public async Task Handle_NoGridAreaOwnershipEvents_ThrowsNotFoundException()
    {
        // Arrange
        var gridAreaRepository = new Mock<IGridAreaRepository>();
        var actorRepository = new Mock<IActorRepository>();
        var target = new GetGridAreaOwnerHandler(gridAreaRepository.Object, actorRepository.Object);

        var gridArea = new GridArea(
            new GridAreaId(Guid.NewGuid()),
            new GridAreaName("name"),
            new GridAreaCode("111"),
            PriceAreaCode.Dk1,
            GridAreaType.Distribution,
            DateTimeOffset.MinValue,
            null);

        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode("111")))
            .ReturnsAsync(gridArea);

        gridAreaRepository
            .Setup(repo => repo.GetGridAreaOwnershipAssignedEventsAsync())
            .Returns(new List<GridAreaOwnershipAssignedEvent>().ToAsyncEnumerable());

        var command = new GetGridAreaOwnerCommand("111");

        // Act + Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => target.Handle(command, CancellationToken.None));
        Assert.Equal($"No owner assigned for grid area: {gridArea.Id} was found", ex.Message);
    }

    [Fact]
    public async Task Handle_NoMatchingGridAreaOwnershipEvents_ThrowsNotFoundException()
    {
        // Arrange
        var gridAreaRepository = new Mock<IGridAreaRepository>();
        var actorRepository = new Mock<IActorRepository>();
        var target = new GetGridAreaOwnerHandler(gridAreaRepository.Object, actorRepository.Object);

        var gridArea = new GridArea(
            new GridAreaId(Guid.NewGuid()),
            new GridAreaName("name"),
            new GridAreaCode("111"),
            PriceAreaCode.Dk1,
            GridAreaType.Distribution,
            DateTimeOffset.MinValue,
            null);

        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode("111")))
            .ReturnsAsync(gridArea);

        var gridAreaOwnership = new GridAreaOwnershipAssignedEvent(
            Guid.NewGuid(),
            new MockedGln(),
            EicFunction.GridAccessProvider,
            new GridAreaId(Guid.NewGuid()),
            NodaTime.Instant.MinValue);
        gridAreaRepository
            .Setup(repo => repo.GetGridAreaOwnershipAssignedEventsAsync())
            .Returns(new[] { gridAreaOwnership }.ToAsyncEnumerable());

        var command = new GetGridAreaOwnerCommand("111");

        // Act + Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => target.Handle(command, CancellationToken.None));
        Assert.Equal($"No owner assigned for grid area: {gridArea.Id} was found", ex.Message);
    }

    [Fact]
    public async Task Handle_FindGridAreaOwner_ReturnsSuccesful()
    {
        // Arrange
        var gridAreaRepository = new Mock<IGridAreaRepository>();
        var actorRepository = new Mock<IActorRepository>();
        var target = new GetGridAreaOwnerHandler(gridAreaRepository.Object, actorRepository.Object);

        var gridArea = new GridArea(
            new GridAreaId(Guid.NewGuid()),
            new GridAreaName("name"),
            new GridAreaCode("111"),
            PriceAreaCode.Dk1,
            GridAreaType.Distribution,
            DateTimeOffset.MinValue,
            null);

        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode("111")))
            .ReturnsAsync(gridArea);

        var actor = new Actor(
        new ActorId(Guid.NewGuid()),
        new OrganizationId(Guid.NewGuid()),
        new MockedGln(),
        new ActorMarketRole(EicFunction.GridAccessProvider, [new ActorGridArea(gridArea.Id)], null),
        new ActorName("Racoon Power"));
        actorRepository
            .Setup(repo => repo.GetActorsAsync())
            .ReturnsAsync(new[] { actor });

        var command = new GetGridAreaOwnerCommand("111");

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.NotNull(response);
        Assert.Equal(actor.ActorNumber.Value, response.GridAccessProviderGln);
    }
}
