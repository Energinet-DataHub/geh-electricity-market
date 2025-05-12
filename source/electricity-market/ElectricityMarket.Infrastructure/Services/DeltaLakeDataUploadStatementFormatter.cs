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
using System.Reflection;
using Energinet.DataHub.ElectricityMarket.Application.Common;
using Energinet.DataHub.ElectricityMarket.Application.Helpers;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class DeltaLakeDataUploadStatementFormatter
{
    private readonly SnakeCaseFormatter _snakeCaseFormatter = new();
    private readonly string _dateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public string CreateUploadStatement<T>(string tableName, IEnumerable<T> dtos)
    {
        var columnNames = GetColumnNames<T>().ToList();
        var columnsString = string.Join(", ", columnNames);

        var valuesString = string.Join(", ", dtos.Select(dto => "(" + string.Join(", ", GetProperties<T>().Select(prop => GetPropertyValue(prop, dto))) + ")"));

        var keyNames = GetKeyNames<T>().ToList();
        var mergeConditionString = string.Join(" AND ", keyNames.Select(key => $"t.{key} = u.{key}"));

        var updateString = string.Join(", ", columnNames.Where(c => !keyNames.Contains(c)).Select(key => $"t.{key} = u.{key}"));
        var insertString = string.Join(", ", columnNames.Select(key => $"u.{key}"));

        return $"""
                with _updates as (
                 SELECT * FROM (
                   VALUES
                     {valuesString}
                 ) A({columnsString})
                )
                MERGE INTO {tableName} t USING _updates u
                ON {mergeConditionString}
                WHEN MATCHED THEN UPDATE SET {updateString}
                WHEN NOT MATCHED BY TARGET THEN INSERT ({columnsString}) VALUES({insertString});
                """;
    }

    private static IEnumerable<PropertyInfo> GetProperties<T>()
    {
        return typeof(T).GetProperties().Where(p => p.CanRead).OrderBy(p => p.Name);
    }

    private string? GetPropertyValue<T>(PropertyInfo prop, T dto)
    {
        if (prop.PropertyType == typeof(DateTimeOffset))
        {
            return "'" + ((DateTimeOffset)prop.GetValue(dto, null)!).ToString(_dateTimeFormat, CultureInfo.InvariantCulture) + "'";
        }

        if (prop.PropertyType == typeof(DateTimeOffset?))
        {
            var value = (DateTimeOffset?)prop.GetValue(dto, null);
            if (value is not null)
            {
                return "'" + ((DateTimeOffset)value).ToString(_dateTimeFormat, CultureInfo.InvariantCulture) + "'";
            }

            return "null";
        }

        return "'" + prop.GetValue(dto, null) + "'";
    }

    private IEnumerable<string> GetKeyNames<T>()
    {
        return typeof(T).GetProperties().Where(p => p.CanRead && p.CustomAttributes.Any(attr => attr.AttributeType == typeof(DeltaLakeKeyAttribute))).Select(p => _snakeCaseFormatter.ToSnakeCase(p.Name));
    }

    private IEnumerable<string> GetColumnNames<T>()
    {
        return GetProperties<T>().Select(p => _snakeCaseFormatter.ToSnakeCase(p.Name));
    }
}
