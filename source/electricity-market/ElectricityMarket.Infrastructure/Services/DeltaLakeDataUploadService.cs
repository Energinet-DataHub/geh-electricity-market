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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class DeltaLakeDataUploadService : IDeltaLakeDataUploadService
{
    private readonly IOptions<DatabricksCatalogOptions> _catalogOptions;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;

    public DeltaLakeDataUploadService(IOptions<DatabricksCatalogOptions> catalogOptions, DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        _catalogOptions = catalogOptions;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
    }

    public async Task ImportTransactionsAsync(IEnumerable<ElectricalHeatingChildDto> electricalHeatingChildren)
    {
        var query = DatabricksStatement.FromRawSql(
            $"""
            INSERT INTO {_catalogOptions.Value.Name}.{_catalogOptions.Value.SchemaName}.{_catalogOptions.Value.ElectricalHeatingChildTableName}
            ({string.Join(", ", GetColumnNames(typeof(ElectricalHeatingChildDto)))})
            VALUES
            {string.Join(",", $"({electricalHeatingChildren.Select(GetValues)})")}
        """);

        try
        {
            var result = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(query.Build());
            await foreach (var record in result.ConfigureAwait(false))
            {
                // Process each record
                Console.WriteLine(record);
                Console.WriteLine(record.num_inserted_rows);
            }

        }
        catch (Exception)
        {
            throw;
        }
    }

    private static string GetValues(ElectricalHeatingChildDto electricalHeatingChild)
    {
        IEnumerable<string> value = [
            electricalHeatingChild.CoupledDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            electricalHeatingChild.MeteringPointId,
            electricalHeatingChild.MeteringPointType,
            electricalHeatingChild.ParentMeteringPointId,
            electricalHeatingChild.SubType,
            electricalHeatingChild.UncoupledDate?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) ?? DateTimeOffset.MaxValue.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
        ];
        return string.Join(", ", value);
    }

    private static IEnumerable<string> GetColumnNames(Type type) =>
        type.GetProperties().Where(p => p.CanRead).Select(p => TransformToSnakeCase(p.Name)).Order();

    private static string TransformToSnakeCase(string propertyName) =>
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
        Regex.Replace(Regex.Replace(propertyName, "(.)([A-Z][a-z]+)", "$1_$2"), "([a-z0-9])([A-Z])", "$1_$2").ToLower(CultureInfo.CurrentCulture);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

}
