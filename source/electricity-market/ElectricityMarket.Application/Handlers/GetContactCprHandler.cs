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
using MediatR;

namespace ElectricityMarket.Application.Handlers;

public sealed class GetContactCprHandler : IRequestHandler<GetContactCprCommand, string>
{
    public async Task<string> Handle(GetContactCprCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // We currently don't have contact data imported, so we return a placeholder value
        return await Task.FromResult("1111111546").ConfigureAwait(false);
    }
}
