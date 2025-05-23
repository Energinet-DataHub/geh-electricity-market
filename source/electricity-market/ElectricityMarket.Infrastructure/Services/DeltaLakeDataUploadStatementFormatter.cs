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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Application.Common;
using Energinet.DataHub.ElectricityMarket.Application.Helpers;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class DeltaLakeDataUploadStatementFormatter
{
    private const int MaxParameters = 256;
    private readonly SnakeCaseFormatter _snakeCaseFormatter = new();
    private readonly DeltaLakeDataUploadParameterFormatter _parameterFormatter = new();

    public IEnumerable<DatabricksStatement> CreateUploadStatementsWithParameters<T>(string tableName, IEnumerable<T> rowObjects)
    {
        var columnNames = GetColumnNames<T>().ToList();
        var columnsString = string.Join(", ", columnNames);

        var countInBatch = MaxParameters / columnNames.Count;

        foreach (var chunk in rowObjects.Chunk(countInBatch))
        {
            var paramIndex = 0;
            var parameters = new List<QueryParameter>();
            var valuesString = string.Join(", ", chunk.Select(dto => "(" + string.Join(
                ", ",
                GetProperties<T>().Select(prop =>
                {
                    var paramName = $"_p{paramIndex++}";
                    var param = _parameterFormatter.GetPropertyValueForParameter(prop, dto, paramName);
                    parameters.Add(param);
                    return $":{paramName}";
                })) + ")"));

            var keyNames = GetKeyNames<T>().ToList();
            var mergeConditionString = string.Join(" AND ", keyNames.Select(key => $"t.{key} = u.{key}"));

            var updateString = string.Join(
                ", ",
                columnNames.Where(c => !keyNames.Contains(c)).Select(key => $"t.{key} = u.{key}"));
            var insertString = string.Join(", ", columnNames.Select(key => $"u.{key}"));

            var queryString = $"""
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

            var builder = DatabricksStatement.FromRawSql(queryString);
            AddParameters(parameters, builder);

            yield return builder.Build();
        }
    }

    public IEnumerable<DatabricksStatement> CreateInsertStatementsWithParameters<T>(string tableName, IEnumerable<T> rowObjects)
    {
        var columnNames = GetColumnNames<T>().ToList();
        var columnsString = string.Join(", ", columnNames);

        var countInBatch = MaxParameters / columnNames.Count;

        foreach (var chunk in rowObjects.Chunk(countInBatch))
        {
            var paramIndex = 0;
            var parameters = new List<QueryParameter>();

            var valuesString = string.Join(", ", chunk.Select(dto => "(" + string.Join(
                ", ",
                GetProperties<T>().Select(prop =>
                {
                    var paramName = $"_p{paramIndex++}";
                    var param = _parameterFormatter.GetPropertyValueForParameter(prop, dto, paramName);
                    parameters.Add(param);
                    return $":{paramName}";
                })) + ")"));

            var queryString = $"""
                               INSERT INTO {tableName} ({columnsString}) VALUES {valuesString}
                               """;

            var builder = DatabricksStatement.FromRawSql(queryString);
            AddParameters(parameters, builder);

            yield return builder.Build();
        }
    }

    public IEnumerable<DatabricksStatement> CreateDeleteStatementsWithParameters<T>(string tableName, IEnumerable<T> rowObjects)
    {
        var columnNames = GetColumnNames<T>().ToList();
        var columnsString = string.Join(", ", columnNames);

        var countInBatch = MaxParameters / columnNames.Count;

        foreach (var chunk in rowObjects.Chunk(countInBatch))
        {
            var paramIndex = 0;
            var parameters = new List<QueryParameter>();
            var valuesString = string.Join(", ", chunk.Select(dto => "(" + string.Join(
                ", ",
                GetProperties<T>().Select(prop =>
                {
                    var paramName = $"_p{paramIndex++}";
                    var param = _parameterFormatter.GetPropertyValueForParameter(prop, dto, paramName);
                    parameters.Add(param);
                    return $":{paramName}";
                })) + ")"));

            var queryString = $"""
                               DELETE FROM {tableName}
                               WHERE ({columnsString}) IN ({valuesString});
                               """;

            var builder = DatabricksStatement.FromRawSql(queryString);
            AddParameters(parameters, builder);

            yield return builder.Build();
        }
    }

    public string CreateUploadStatement<T>(string tableName, IEnumerable<T> rowObjects)
    {
        var columnNames = GetColumnNames<T>().ToList();
        var columnsString = string.Join(", ", columnNames);

        var valuesString = string.Join(", ", rowObjects.Select(dto => "(" + string.Join(", ", GetProperties<T>().Select(prop => _parameterFormatter.GetPropertyValue(prop, dto))) + ")"));

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

    public string CreateDeleteStatement<T>(string tableName, IEnumerable<T> rowObjects)
    {
        var valuesString = string.Join(", ", rowObjects.Select(dto => "(" + string.Join(", ", GetKeys<T>().Select(prop => _parameterFormatter.GetPropertyValue(prop, dto))) + ")"));

        var keyNames = GetKeyNames<T>().ToList();
        var keyColumnsString = string.Join(", ", keyNames);

        return $"""
                DELETE FROM {tableName}
                WHERE ({keyColumnsString}) IN ({valuesString});
                """;
    }

    private static void AddParameters(List<QueryParameter> parameters, DatabricksStatementBuilder builder)
    {
        // HACK: Use reflection to access private field _queryParameters in DatabricksStatementBuilder class. A fix for this is available in geh-core version > 13.1.0
        foreach (var p in parameters)
        {
            var fieldInfo = builder.GetType().GetField("_queryParameters", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var parameterList = (List<QueryParameter>)fieldInfo.GetValue(builder)!;
            parameterList.Add(p);
        }
    }

    private static IEnumerable<PropertyInfo> GetProperties<T>()
    {
        return typeof(T).GetProperties().Where(p => p.CanRead).OrderBy(p => p.Name);
    }

    private static IEnumerable<PropertyInfo> GetKeys<T>()
    {
        return typeof(T).GetProperties().Where(p => p.CanRead && p.CustomAttributes.Any(attr => attr.AttributeType == typeof(DeltaLakeKeyAttribute)));
    }

    private IEnumerable<string> GetKeyNames<T>()
    {
        return GetKeys<T>().Select(p => _snakeCaseFormatter.ToSnakeCase(p.Name));
    }

    private IEnumerable<string> GetColumnNames<T>()
    {
        return GetProperties<T>().Select(p => _snakeCaseFormatter.ToSnakeCase(p.Name));
    }
}
