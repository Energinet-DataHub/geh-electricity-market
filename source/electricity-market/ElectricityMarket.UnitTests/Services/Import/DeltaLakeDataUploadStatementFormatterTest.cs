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
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Energinet.DataHub.ElectricityMarket.Integration.Models.Common;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Services.Import;

public class DeltaLakeDataUploadStatementFormatterTest
{
    private string _devTableName = "ctl_shres_d_we_002.electricity_market_internal.electrical_heating_child_metering_points";

    [Fact(Skip = "Manual")]
    public void GivenTestDto_WhenCreatingStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var statement = sut.CreateUploadStatement<TestDto>("testCatalog.TestTable", [new TestDto("123", "abc", DateTimeOffset.Now), new TestDto("123", "abc", null)]);
    }

    [Fact(Skip = "Manual")]
    public void GivenElectricalHeatingChildDto_WhenCreatingStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var dto = new ElectricalHeatingChildDto("570714700000002601", "Consumption", "Physical", "570714700000004704", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var statement = sut.CreateUploadStatement<ElectricalHeatingChildDto>(_devTableName, [dto]);
    }

    [Fact(Skip = "Manual")]
    public void GivenListOfElectricalHeatingChildDtos_WhenCreatingStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var dto1 = new ElectricalHeatingChildDto("570714700000002601", "Consumption", "Physical", "570714700000004704", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var dto2 = new ElectricalHeatingChildDto("570714700000002602", "Consumption", "Physical", "570714700000004704", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var dto3 = new ElectricalHeatingChildDto("570714700000002603", "Consumption", "Physical", "570714700000004704", DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var statement = sut.CreateUploadStatement<ElectricalHeatingChildDto>(_devTableName, [dto1, dto2, dto3]);
    }

    public sealed record TestDto([property: DeltaLakeKey] string Id, string Prop, DateTimeOffset? NullableTimestamp);
}
