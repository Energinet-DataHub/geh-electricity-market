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
using System.Net;
using Energinet.DataHub.ElectricityMarket.Application.Commands.Authorize;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.ElectricityMarket.Hosts.DataApi.Functions.Authorize;
internal sealed class VerifyGridOwnerTrigger
{
    private readonly IMediator _mediator;

    public VerifyGridOwnerTrigger(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function(nameof(VerifyGridOwnerAsync))]
    [Authorize]
    public async Task<HttpResponseData> VerifyGridOwnerAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "verify-grid-owner")]
        HttpRequestData req,
        [FromBody] ReadOnlyCollection<string> gridAreaCodes,
        FunctionContext executionContext)
    {
        var indentification = req.Query["identification"];
        if (string.IsNullOrWhiteSpace(indentification))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            return badRequestResponse;
        }

        var command = new VerifyGridOwnerCommand(indentification, gridAreaCodes);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response
            .WriteAsJsonAsync(result)
            .ConfigureAwait(false);

        return response;
    }
}
