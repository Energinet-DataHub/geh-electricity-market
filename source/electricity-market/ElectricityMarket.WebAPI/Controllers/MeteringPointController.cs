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

using ElectricityMarket.WebAPI.Revision;
using ElectricityMarket.WebAPI.Security;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Contacts;
using Energinet.DataHub.ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
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

    [HttpGet("{identification}")]
    [EnableRevision(RevisionActivities.MeteringPointRequested, typeof(MeteringPoint), "identification")]
    public async Task<ActionResult<MeteringPointMasterDataDto>> GetMeteringPointAsync(string identification, [FromQuery] TenantDto tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (tenant.MarketRole != EicFunction.DataHubAdministrator)
        {
            return Unauthorized();
        }

        var getMeteringPointCommand = new GetMeteringPointCommand(identification);

        var meteringPoint = await _mediator
            .Send(getMeteringPointCommand)
            .ConfigureAwait(false);

        return Ok(meteringPoint.MeteringPoint);
    }

    [HttpGet("contact/{contactId:long}/cpr")]
    [EnableRevision(RevisionActivities.ContactCprRequested, typeof(MeteringPoint), "contactId")]
    public async Task<ActionResult<string>> GetContactCprAsync(long contactId, [FromBody] ContactCprRequestDto contactCprRequest)
    {
        var command = new GetContactCprCommand(contactId, contactCprRequest);

        var cpr = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(cpr);
    }
}
