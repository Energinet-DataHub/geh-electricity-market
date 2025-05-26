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

using System.Collections.ObjectModel;
using ElectricityMarket.WebAPI.Model;
using ElectricityMarket.WebAPI.Revision;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Authorize;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Contacts;
using Energinet.DataHub.ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Application.Security;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.RevisionLog.Integration.WebApi;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityMarket.WebAPI.Controllers;

[ApiController]
[Route("metering-point")]
public class MeteringPointController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext<FrontendUser> _userContext;

    public MeteringPointController(IMediator mediator, IUserContext<FrontendUser> userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    [HttpGet("{identification}")]
    [EnableRevision(RevisionActivities.MeteringPointRequested, typeof(MeteringPoint), "identification")]
    [Authorize(Roles = "metering-point:search")]
    public async Task<ActionResult<MeteringPointDto>> GetMeteringPointAsync(string identification)
    {
        var tenant = new TenantDto(_userContext.CurrentUser.ActorNumber, _userContext.CurrentUser.MarketRole);
        var getMeteringPointCommand = new GetMeteringPointCommand(identification, tenant);

        var meteringPoint = await _mediator
            .Send(getMeteringPointCommand)
            .ConfigureAwait(false);

        if (meteringPoint == null)
        {
            return NotFound();
        }

        return Ok(meteringPoint.MeteringPoint);
    }

    [HttpPost("{identification}/verify-grid-owner")]
    [EnableRevision(RevisionActivities.VerifyGridOwnerRequested, typeof(MeteringPoint), "meteringPointId")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> VerifyGridOwnerAsync(string meteringPointId, [FromBody] ReadOnlyCollection<string> gridAreaCodes)
    {
        var verifyGridOwnerCommand = new VerifyGridOwnerCommand(meteringPointId, gridAreaCodes);

        var meteringPoint = await _mediator
            .Send(verifyGridOwnerCommand)
            .ConfigureAwait(false);

        if (!meteringPoint)
        {
            return NotFound();
        }

        return Ok(meteringPoint);
    }

    [HttpGet("{identification}/related")]
    [EnableRevision(RevisionActivities.RelatedMeteringPointsRequested, typeof(MeteringPoint), "identification")]
    [Authorize(Roles = "metering-point:search")]
    public async Task<ActionResult<RelatedMeteringPointsDto>> GetRelatedMeteringPointAsync(string identification)
    {
        var getRelatedCommand = new GetRelatedMeteringPointsCommand(identification);

        var meteringPointWithRelated = await _mediator
            .Send(getRelatedCommand)
            .ConfigureAwait(false);

        if (meteringPointWithRelated == null)
        {
            return NotFound();
        }

        return Ok(meteringPointWithRelated.RelatedMeteringPoints);
    }

    [HttpGet("contact/{contactId:long}/cpr")]
    [EnableRevision(RevisionActivities.ContactCprRequested, typeof(MeteringPoint), "contactId")]
    [Authorize(Roles = "cpr:view")]
    public async Task<ActionResult<CPRResponse>> GetContactCprAsync(long contactId, [FromBody] ContactCprRequestDto contactCprRequest)
    {
        var command = new GetContactCprCommand(contactId, contactCprRequest);

        var cpr = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(new { result = cpr });
    }

    [HttpGet("{identification}/debug-view")]
    [EnableRevision(RevisionActivities.MeteringPointRequested, typeof(MeteringPoint), "identification")]
    public async Task<ActionResult<DebugResponse>> GetMeteringPointDebugViewAsync(string identification)
    {
        if (!_userContext.CurrentUser.IsFas)
        {
            return Unauthorized();
        }

        var command = new GetMeteringPointDebugViewCommand(identification);

        var meteringPoint = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok(new { result = meteringPoint.MeteringPoint });
    }

    [HttpGet("debug")]
    [EnableRevision(RevisionActivities.MeteringPointRequested, typeof(MeteringPoint), "gridAreaCode")]
    public async Task<ActionResult<IEnumerable<MeteringPointDto>>> GetMeteringPointsByGridAreaCodeAsync([FromQuery] string gridAreaCode)
    {
        if (!_userContext.CurrentUser.IsFas)
        {
            return Unauthorized();
        }

        var command = new GetMeteringPointByGridAreaCodeCommand(gridAreaCode);

        var meteringPoints = (await _mediator
            .Send(command)
            .ConfigureAwait(false)).MeteringPoints;

        return Ok(meteringPoints);
    }
}
