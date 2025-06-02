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

using System;
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Common;

internal static class Some
{
    public static string IntString(int charCount)
    {
        var alphabet = "0123456789";
        var random = new Random();
#pragma warning disable CA5394
        var characterSelector = new Func<int, string>(_ => alphabet.Substring(random.Next(0, alphabet.Length), 1));
#pragma warning restore CA5394
        return Enumerable.Range(1, charCount).Select(characterSelector).Aggregate((a, b) => a + b);
    }

    public static MeteringPointIdentification MeteringPointIdentification()
    {
        return new MeteringPointIdentification(IntString(18));
    }

    public static InstallationAddressEntity InstallationAddressEntity()
    {
        return new InstallationAddressEntity()
        {
            StreetName = "123 Main Street",
            BuildingNumber = IntString(3),
            CityName = "New York",
            CountryCode = "US",
            PostCode = IntString(5),
        };
    }
}
