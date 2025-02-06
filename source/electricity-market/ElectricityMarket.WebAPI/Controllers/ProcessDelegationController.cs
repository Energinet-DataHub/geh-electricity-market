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

using ElectricityMarket.Application.Commands.ProcessDelegations;
using ElectricityMarket.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityMarket.WebAPI.Controllers;

[ApiController]
[Route("process-delegation")]
public class ProcessDelegationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProcessDelegationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("get-by")]
    public async Task<ActionResult<ProcessDelegationDto>> GetProcessesDelegatedByAsync([FromBody] ProcessDelegationRequestDto processDelegationRequest)
    {
        var command = new GetProcessDelegationCommand(processDelegationRequest);

        var response = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(response);
    }
}
