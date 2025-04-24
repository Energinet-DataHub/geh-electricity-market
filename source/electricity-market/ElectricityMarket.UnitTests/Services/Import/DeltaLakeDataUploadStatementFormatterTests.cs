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
using Energinet.DataHub.ElectricityMarket.Application.Common;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Services.Import;

public class DeltaLakeDataUploadStatementFormatterTests
{
    private readonly string _electricalHeatingDevTableName = "ctl_shres_d_we_002.electricity_market_internal.electrical_heating_child_metering_points";
    private readonly string _capacitySettlementDevTableName = "ctl_shres_d_we_002.electricity_market_internal.capacity_settlement_metering_point_periods";

    [Fact]
    public void GivenTestDto_WhenCreatingStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var statement = sut.CreateUploadStatement<TestDto>("testCatalog.TestTable", [new TestDto("123", null, new DateTimeOffset(2019, 3, 25, 12, 13, 14, TimeSpan.FromHours(1))), new TestDto("123", "abc", null)]);
        var expectedStatement = """
                                 with _updates as (
                                  SELECT * FROM (
                                    VALUES
                                      ('123', to_timestamp('2019-03-25T12:13:14Z'), null), ('123', to_timestamp(null), 'abc')
                                  ) A(id, nullable_timestamp, prop)
                                 )
                                 MERGE INTO testCatalog.TestTable t USING _updates u
                                 ON t.id = u.id
                                 WHEN MATCHED THEN UPDATE SET t.nullable_timestamp = u.nullable_timestamp, t.prop = u.prop
                                 WHEN NOT MATCHED BY TARGET THEN INSERT (id, nullable_timestamp, prop) VALUES(u.id, u.nullable_timestamp, u.prop);
                                 """;
        Assert.Equal(expectedStatement, statement);
    }

    [Fact]
    public void GivenTestDto_WhenCreatingStatementWithParameters_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var statement = sut.CreateUploadStatementWithParameters<TestDto>("testCatalog.TestTable", [new TestDto("123", null, new DateTimeOffset(2019, 3, 25, 12, 13, 14, TimeSpan.FromHours(1))), new TestDto("123", "abc", null)]);
        var expectedStatement = """
                                with _updates as (
                                 SELECT * FROM (
                                   VALUES
                                     (:p0, :p1, :p2), (:p3, :p4, :p5)
                                 ) A(id, nullable_timestamp, prop)
                                )
                                MERGE INTO testCatalog.TestTable t USING _updates u
                                ON t.id = u.id
                                WHEN MATCHED THEN UPDATE SET t.nullable_timestamp = u.nullable_timestamp, t.prop = u.prop
                                WHEN NOT MATCHED BY TARGET THEN INSERT (id, nullable_timestamp, prop) VALUES(u.id, u.nullable_timestamp, u.prop);
                                """;
        Assert.Equal(expectedStatement, statement.ToString());
    }

    [Fact]
    public void GivenTestDto_WhenCreatingDeleteStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var statement = sut.CreateDeleteStatement<MultiKeyTestDto>("testCatalog.TestTable", [new MultiKeyTestDto("123", "abc", "fiz"), new MultiKeyTestDto("789", "hij", "buz")]);
        var expectedStatement = """
                                DELETE FROM testCatalog.TestTable
                                WHERE (id, id2) IN (('123', 'abc'), ('789', 'hij'));
                                """;
        Assert.Equal(expectedStatement, statement);
    }

    [Fact]
    public void GivenTestDto_WhenCreatingDeleteStatementWithParameters_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var statement = sut.CreateDeleteStatementWithParameters<MultiKeyTestDto>("testCatalog.TestTable", [new MultiKeyTestDto("123", "abc", "fiz"), new MultiKeyTestDto("789", "hij", "buz")]);
        var expectedStatement = """
                                DELETE FROM testCatalog.TestTable
                                WHERE (id, id2) IN ((:p0, :p1), (:p2, :p3));
                                """;
        Assert.Equal(expectedStatement, statement.ToString());
    }

    [Fact(Skip = "Manual")]
    public void GivenElectricalHeatingChildDto_WhenCreatingStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var dto = new ElectricalHeatingChildDto(570714700000002601, "Consumption", "Physical", 570714700000004704, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var statement = sut.CreateUploadStatement<ElectricalHeatingChildDto>(_electricalHeatingDevTableName, [dto]);
    }

    [Fact(Skip = "Manual")]
    public void GivenListOfElectricalHeatingChildDtos_WhenCreatingStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var dto1 = new ElectricalHeatingChildDto(570714700000002601, "Consumption", "Physical", 570714700000004704, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var dto2 = new ElectricalHeatingChildDto(570714700000002602, "Consumption", "Physical", 570714700000004704, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var dto3 = new ElectricalHeatingChildDto(570714700000002603, "Consumption", "Physical", 570714700000004704, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var statement = sut.CreateUploadStatement<ElectricalHeatingChildDto>(_electricalHeatingDevTableName, [dto1, dto2, dto3]);
    }

    [Fact(Skip = "Manual")]
    public void GivenListOfCapacitySettlementPeriodDtos_WhenCreatingStatement_StatementIsCreated()
    {
        var sut = new DeltaLakeDataUploadStatementFormatter();
        var dto1 = new CapacitySettlementPeriodDto(570714700000002601, DateTimeOffset.Now.AddDays(-10), DateTimeOffset.Now.AddDays(10), "570714700000002611", DateTimeOffset.Now.AddDays(-2), DateTimeOffset.Now.AddDays(2));
        var dto2 = new CapacitySettlementPeriodDto(570714700000002602, DateTimeOffset.Now.AddDays(-10), null, "570714700000002612", DateTimeOffset.Now.AddDays(-2), null);
        var dto3 = new CapacitySettlementPeriodDto(570714700000002603, DateTimeOffset.Now.AddDays(-10), DateTimeOffset.Now.AddDays(10), "570714700000002623", DateTimeOffset.Now.AddDays(-2), DateTimeOffset.Now.AddDays(2));
        var statement = sut.CreateUploadStatement<CapacitySettlementPeriodDto>(_capacitySettlementDevTableName, [dto1, dto2, dto3]);
    }

    private sealed record TestDto([property: DeltaLakeKey] string Id, string? Prop, DateTimeOffset? NullableTimestamp);
    private sealed record MultiKeyTestDto([property: DeltaLakeKey] string Id, [property: DeltaLakeKey] string Id2, string Prop);
}
