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
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using ElectricityMarket.Application.Commands.MeteringPoints;
using ElectricityMarket.Application.Handlers;
using ElectricityMarket.Domain.Models;
using ElectricityMarket.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Handlers;

[UnitTest]
public sealed class GetMeteringPointDataHandlerTests
{
    [Fact]
    public async Task Handle_NoMeteringPoint_ThrowsNotFoundException()
    {
        // Arrange
        var meteringPointRepository = new Mock<IMeteringPointRepository>();
        var target = new GetMeteringPointDataHandler(meteringPointRepository.Object);

        meteringPointRepository
            .Setup(repo => repo.GetAsync(new MeteringPointIdentification(It.IsAny<string>())))
            .ReturnsAsync((MeteringPoint?)null);

        var command = new GetMeteringPointDataCommand("XXXXXX");

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FindsMeteringPoint_ReturnsMeteringPoint()
    {
        // Arrange
        var meteringPointRepository = new Mock<IMeteringPointRepository>();
        var target = new GetMeteringPointDataHandler(meteringPointRepository.Object);

        var meteringPointIdentification = "123456789012345678";
        var meteringPoint = new MeteringPoint(
            123,
            new MeteringPointIdentification(meteringPointIdentification),
            [],
            []);
        meteringPointRepository
            .Setup(repo => repo.GetAsync(new MeteringPointIdentification(meteringPointIdentification)))
            .ReturnsAsync(meteringPoint);

        var command = new GetMeteringPointDataCommand(meteringPointIdentification);

        // Act
        var response = await target.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response.MeteringPointData);
        Assert.Equal(meteringPointIdentification, response.MeteringPointData.Identification);
    }
}
