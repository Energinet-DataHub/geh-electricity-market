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
using NodaTime;

namespace Energinet.DataHub.ElectricityMarket.Hosts.DataApi.Functions.Authorize
{
    internal sealed class GetSupplierPeriodsTrigger
    {
        private readonly IMediator _mediator;

        public GetSupplierPeriodsTrigger(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Function(nameof(GetSupplierPeriodsTriggerAsync))]
        [Authorize]
        public async Task<HttpResponseData> GetSupplierPeriodsTriggerAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "get-supplier-periods")]
        HttpRequestData req,
        [FromBody] Interval requestedPeriod,
        FunctionContext executionContext)
        {
            var meteringPointId = req.Query["meteringPointId"];
            var actorNumber = req.Query["actorNumber"];
            if (string.IsNullOrWhiteSpace(meteringPointId) || string.IsNullOrWhiteSpace(actorNumber))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                return badRequestResponse;
            }

            var command = new GetSupplierPeriodsCommand(meteringPointId, actorNumber, requestedPeriod);

            var result = await _mediator
                .Send(command)
                .ConfigureAwait(false);

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response
                .WriteAsJsonAsync(result)
                .ConfigureAwait(false);

            return response;
        }
    }
}
