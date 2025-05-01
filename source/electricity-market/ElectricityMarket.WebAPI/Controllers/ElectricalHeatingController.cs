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

using ElectricityMarket.WebAPI.Model;
using ElectricityMarket.WebAPI.Revision;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Contacts;
using Energinet.DataHub.ElectricityMarket.Application.Commands.DeltaLakeSync;
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
public class ElectricalHeatingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext<FrontendUser> _userContext;

    public ElectricalHeatingController(IMediator mediator, IUserContext<FrontendUser> userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetAsync()
    {
        var command = new SyncElectricalHeatingCommand();

        await _mediator
            .Send(command)
            .ConfigureAwait(false);

        return Ok();
    }
}
