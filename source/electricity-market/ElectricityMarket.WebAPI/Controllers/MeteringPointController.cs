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
using ElectricityMarket.Domain.Models;
using ElectricityMarket.WebAPI.Revision;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityMarket.WebAPI.Controllers;

[ApiController]
[Route("metering-point")]
public class MeteringPointController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeteringPointController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("contact/{contactId:long}/cpr")]
    [EnableRevision(RevisionActivities.ContactCprRequested, typeof(MeteringPoint), "contactId")]
    public async Task<ActionResult<string>> GetContactCprAsync(long contactId, [FromBody]ContactCprRequestDto contactCprRequest)
    {
        var command = new GetContactCprCommand(contactId, contactCprRequest);

        var cpr = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(cpr);
    }
}
