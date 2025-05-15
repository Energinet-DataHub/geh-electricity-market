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
using System.Reflection;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Services.Import;

public class DeltaLakeDataUploadParameterFormatterTest
{
    private DeltaLakeDataUploadParameterFormatter _sut = new();

    [Fact]
    public void GivenDto_WhenGettingParameterValueForStringStatement_ParameterValuesAreReturned()
    {
        var dto = new TestDto("123abc", null, new DateTimeOffset(2023, 4, 3, 12, 5, 56, TimeSpan.FromHours(2)), null);

        var paramValue = _sut.GetPropertyValue(GetPropertyInfo(nameof(TestDto.Id)), dto);
        Assert.Equal("'123abc'", paramValue);

        paramValue = _sut.GetPropertyValue(GetPropertyInfo(nameof(TestDto.Prop)), dto);
        Assert.Equal("null", paramValue);

        paramValue = _sut.GetPropertyValue(GetPropertyInfo(nameof(TestDto.Timestamp)), dto);
        Assert.Equal("to_timestamp('2023-04-03T10:05:56Z')", paramValue);

        paramValue = _sut.GetPropertyValue(GetPropertyInfo(nameof(TestDto.NullableTimestamp)), dto);
        Assert.Equal("to_timestamp(null)", paramValue);
    }

    [Fact]
    public void GivenDto_WhenGettingParameterValueForDeltaParameters_ParameterValuesAreReturned()
    {
        var dto = new TestDto("123abc", null, new DateTimeOffset(2023, 4, 3, 12, 5, 56, TimeSpan.FromHours(2)), null);

        var paramValue = _sut.GetPropertyValueForParameter(GetPropertyInfo(nameof(TestDto.Id)), dto);
        Assert.Equal("123abc", paramValue);

        paramValue = _sut.GetPropertyValueForParameter(GetPropertyInfo(nameof(TestDto.Prop)), dto);
        Assert.Equal("null", paramValue);

        paramValue = _sut.GetPropertyValueForParameter(GetPropertyInfo(nameof(TestDto.Timestamp)), dto);
        Assert.Equal("2023-04-03T10:05:56Z", paramValue);

        paramValue = _sut.GetPropertyValueForParameter(GetPropertyInfo(nameof(TestDto.NullableTimestamp)), dto);
        Assert.Equal("null", paramValue);
    }

    private static PropertyInfo GetPropertyInfo(string propertyName)
    {
        return typeof(TestDto).GetProperty(propertyName)!;
    }

    private sealed record TestDto(string Id, string? Prop, DateTimeOffset Timestamp, DateTimeOffset? NullableTimestamp);
}
