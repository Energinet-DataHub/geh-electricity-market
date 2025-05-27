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

using Energinet.DataHub.ElectricityMarket.Application.Commands.DeltaLakeSync;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityMarket.WebAPI.Controllers;

[ApiController]
[Route("sync")]
public class SyncController : ControllerBase
{
    private readonly IMediator _mediator;

    public SyncController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("electrical-heating")]
    public async Task<ActionResult> GetAsync()
    {
        var command = new SyncElectricalHeatingCommand();

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("capacity-settlement")]
    public async Task<ActionResult> StartCapacitySettlementSyncAsync()
    {
        var command = new SyncCapacitySettlementCommand();

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("net-consumption")]
    public async Task<ActionResult> StartNetConsumptionSyncAsync()
    {
        var command = new SyncNetConsumptionCommand();

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpGet("hullerlog")]
    public async Task<ActionResult> StartHullerLogSyncAsync()
    {
        var command = new SyncHullerLogCommand();

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }
}
