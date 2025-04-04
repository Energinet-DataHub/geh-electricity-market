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
using System.Globalization;
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Integration.Models.MasterData;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Model;

[UnitTest]
public class EnumTests
{
    [Fact]
    public void EnumsShouldBeIdentical()
    {
        var integrationAssembly = typeof(MeteringPointType).Assembly;
        var modelAssembly = typeof(Energinet.DataHub.ElectricityMarket.Domain.Models.MeteringPointType).Assembly;

        var integrationEnums = integrationAssembly.GetTypes()
            .Where(t => t.IsEnum)
            .ToDictionary(t => t.Name);

        var modelEnums = modelAssembly.GetTypes()
            .Where(t => t.IsEnum)
            .ToDictionary(t => t.Name);

        var common = integrationEnums.Keys.Intersect(modelEnums.Keys);

        foreach (var name in common)
        {
            var integrationEnumDict = Enum.GetValues(integrationEnums[name])
                .Cast<Enum>()
                .OrderBy(x => x.ToString())
                .Select(
                    x => (name: x.ToString(), value: Convert.ToInt32(x, CultureInfo.InvariantCulture)))
                .OrderBy(x => x.value)
                .ToList();

            var modelEnumDict = Enum.GetValues(modelEnums[name])
                .Cast<Enum>()
                .Select(
                    x => (name: x.ToString(), value: Convert.ToInt32(x, CultureInfo.InvariantCulture)))
                .OrderBy(x => x.value)
                .ToList();

            if (!integrationEnumDict.SequenceEqual(modelEnumDict))
            {
                Assert.Fail($"""

                             {name} differ

                                Integration:
                                    {string.Join("\n".PadRight(8), integrationEnumDict.Select(x => $"{x.name} = {x.value}"))}

                                Model:
                                    {string.Join("\n".PadRight(8), modelEnumDict.Select(x => $"{x.name} = {x.value}"))}

                             """);
            }
        }
    }
}
