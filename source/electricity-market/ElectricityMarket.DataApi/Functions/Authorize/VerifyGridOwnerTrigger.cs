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
using Energinet.DataHub.ElectricityMarket.Application.Commands.MasterData;
using Energinet.DataHub.ElectricityMarket.Application.Commands.MeteringPoints;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
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
        [FromBody] ReadOnlyCollection<string> gridAreaCode,
        FunctionContext executionContext)
    {
        // Ensure the "identification" query parameter is not null or empty
        if (string.IsNullOrWhiteSpace(req.Query["identification"]))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            // await badRequestResponse.WriteStringAsync("The 'identification' query parameter is required.").ConfigureAwait(false);
            return badRequestResponse;
        }

#pragma warning disable CS8604 // Possible null reference argument.
        var command = new VerifyGridOwnerCommand(req.Query["identification"], gridAreaCode);
#pragma warning restore CS8604 // Possible null reference argument.

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
