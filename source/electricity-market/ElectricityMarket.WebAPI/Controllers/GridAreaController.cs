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

using ElectricityMarket.Application.Commands.Contacts;
using ElectricityMarket.Application.Commands.GridArea;
using ElectricityMarket.Application.Commands.MasterData;
using ElectricityMarket.Application.Models;
using ElectricityMarket.Domain.Models;
using ElectricityMarket.Domain.Models.GridArea;
using ElectricityMarket.WebAPI.Revision;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityMarket.WebAPI.Controllers;

[ApiController]
[Route("grid-area")]
public class GridAreaController : ControllerBase
{
    private readonly IMediator _mediator;

    public GridAreaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("owner/{gridAreaCode}")]
    public async Task<ActionResult<GridAreaOwnerDto>> GetMeteringPointMasterDataChangesAsync(string gridAreaCode)
    {
        var command = new GetGridAreaOwnerCommand(gridAreaCode);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response);
    }
}
