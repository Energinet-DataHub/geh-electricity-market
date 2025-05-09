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

using Energinet.DataHub.ElectricityMarket.Application.Helpers;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Helpers;

public class SnakeCaseFormatterTest
{
    private readonly SnakeCaseFormatter _sut = new();

    [Theory]
    [InlineData("", "")]
    [InlineData("A", "a")]
    [InlineData("a", "a")]
    [InlineData("AB", "a_b")]
    [InlineData("abc", "abc")]
    [InlineData("ab c", "ab c")]
    [InlineData("Abc__Def", "abc_def")]
    [InlineData("already_snake_case", "already_snake_case")]
    [InlineData("NotAlreadySnakeCase", "not_already_snake_case")]
    [InlineData("SpacesAnd Stuff", "spaces_and_stuff")]
    public void ConvertToSnakeCase(string input, string expectedOutput)
    {
        var output = _sut.ToSnakeCase(input);
        Assert.Equal(expectedOutput, output);
    }
}
