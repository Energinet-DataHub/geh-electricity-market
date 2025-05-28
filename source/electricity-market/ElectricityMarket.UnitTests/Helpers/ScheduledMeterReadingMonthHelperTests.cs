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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Helpers;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Helpers;

public class ScheduledMeterReadingMonthHelperTests
{
    [Theory]
    [InlineData("0131", 2)]
    [InlineData("0228", 3)]
    [InlineData("0229", 3)]
    [InlineData("1231", 1)]
    public void ConvertToSingleMonth_GivenCorrectInput_ReturnsExpectedMonth(string input, int expectedMonth)
    {
        Assert.Equal(expectedMonth, ScheduledMeterReadingMonthHelper.ConvertToSingleMonth(input));
    }
}
