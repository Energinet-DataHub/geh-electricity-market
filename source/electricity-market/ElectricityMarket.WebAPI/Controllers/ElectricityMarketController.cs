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

using ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityMarket.WebAPI.Controllers;

[ApiController]
[Route("electricity-market")]
public class ElectricityMarketController : ControllerBase
{
    private readonly IMediator _mediator;

    public ElectricityMarketController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{meteringPointIdentification}")]
    public async Task<ActionResult<MeteringPointDto>> GetMeteringPointDataAsync(string meteringPointIdentification)
    {
        var getMeteringPointDataCommand = new GetMeteringPointDataCommand(meteringPointIdentification);

        var meteringPoint = await _mediator
            .Send(getMeteringPointDataCommand)
            .ConfigureAwait(false);

        return Ok(meteringPoint.MeteringPointData);
    }

    [HttpGet("cpr/{contactId}")]
    public async Task<ActionResult<MeteringPointDto>> GetMeteringPointDataAsync(long contactId)
    {
        var getMeteringPointDataCommand = new GetContactCprCommand(contactId);

        var cpr = await _mediator
            .Send(getMeteringPointDataCommand)
            .ConfigureAwait(false);

        return Ok(cpr);
    }
}
