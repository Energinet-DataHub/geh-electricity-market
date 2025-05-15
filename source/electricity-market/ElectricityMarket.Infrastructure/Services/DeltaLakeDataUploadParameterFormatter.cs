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
using System.Reflection;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class DeltaLakeDataUploadParameterFormatter
{
    private const string NullString = "null";
    private const string VoidTypeString = "VOID";
    private const string TimestampNtzTypeString = "TIMESTAMP_NTZ";

    private readonly string _dateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public string GetPropertyValue<T>(PropertyInfo prop, T dto)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var propertyValue = prop.GetValue(dto, null);
        if (propertyValue is null)
        {
            if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
            {
                return $"to_timestamp({NullString})";
            }

            return NullString;
        }

        if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
        {
            var propValuesAsDateTimeOffset = (DateTimeOffset)propertyValue;

            if (propValuesAsDateTimeOffset == DateTimeOffset.MaxValue)
            {
                return $"to_timestamp({NullString})";
            }

            return $"to_timestamp('{propValuesAsDateTimeOffset.ToUniversalTime().ToString(_dateTimeFormat, CultureInfo.InvariantCulture)}')";
        }

        return $"'{propertyValue}'";
    }

    public QueryParameter GetPropertyValueForParameter<T>(PropertyInfo prop, T dto, string paramName)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var propertyValue = prop.GetValue(dto, null);
        if (propertyValue is null)
        {
            if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
            {
                return CreateNullParameter(paramName);
            }

            return QueryParameter.Create(paramName, NullString);
        }

        if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
        {
            var propValuesAsDateTimeOffset = (DateTimeOffset)propertyValue;

            if (propValuesAsDateTimeOffset == DateTimeOffset.MaxValue)
            {
                return CreateNullParameter(paramName);
            }

            return CreateTimestampParameter(paramName, $"{propValuesAsDateTimeOffset.ToUniversalTime().ToString(_dateTimeFormat, CultureInfo.InvariantCulture)}");
        }

        return QueryParameter.Create(paramName, $"{propertyValue}");
    }

    private static QueryParameter CreateNullParameter(string paramName)
    {
        // HACK: Use reflection to access private constructor in QueryParameter class. A fix for this is available in geh-core version > 13.1.0
        var constructorInfo = typeof(QueryParameter).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        return (QueryParameter)constructorInfo.Invoke([paramName, string.Empty, VoidTypeString]);
    }

    private static QueryParameter CreateTimestampParameter(string paramName, string value)
    {
        // HACK: Use reflection to access private constructor in QueryParameter class. A fix for this is available in geh-core version > 13.1.0
        var constructorInfo = typeof(QueryParameter).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        return (QueryParameter)constructorInfo.Invoke([paramName, value, TimestampNtzTypeString]);
    }
}
