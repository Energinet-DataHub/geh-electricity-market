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

using System.Net;
using Energinet.DataHub.ElectricityMarket.Application.Commands.GridArea;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Energinet.DataHub.ElectricityMarket.Hosts.DataApi.Functions;

internal sealed class GridAreaTrigger
{
    private readonly IMediator _mediator;

    public GridAreaTrigger(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function(nameof(GetGridAreaAsync))]
    public async Task<HttpResponseData> GetGridAreaAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "get-grid-area-owner")]
        HttpRequestData req,
        string gridAreaCode,
        FunctionContext executionContext)
    {
        var command = new GetGridAreaOwnerCommand(gridAreaCode);

        var result = await _mediator
            .Send(command)
            .ConfigureAwait(false);

        HttpResponseData response;
        if (result != null)
        {
            response = req.CreateResponse(HttpStatusCode.OK);
            await response
                .WriteAsJsonAsync(result)
                .ConfigureAwait(false);
        }
        else
        {
            response = req.CreateResponse(HttpStatusCode.NotFound);
        }

        return response;
    }
}
