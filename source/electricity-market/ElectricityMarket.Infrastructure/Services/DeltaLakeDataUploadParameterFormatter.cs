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
using System.Reflection;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class DeltaLakeDataUploadParameterFormatter
{
    private readonly string _dateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public string GetPropertyValue<T>(PropertyInfo prop, T dto)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var propertyValue = prop.GetValue(dto, null);
        if (propertyValue is null)
        {
            if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
            {
                return "to_timestamp(null)";
            }

            return "null";
        }

        if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
        {
            return $"to_timestamp('{((DateTimeOffset)propertyValue).ToUniversalTime().ToString(_dateTimeFormat, CultureInfo.InvariantCulture)}')";
        }

        return $"'{propertyValue}'";
    }

    public string GetPropertyValueForParameter<T>(PropertyInfo prop, T dto)
    {
        ArgumentNullException.ThrowIfNull(prop);

        var propertyValue = prop.GetValue(dto, null);
        if (propertyValue is null)
        {
            return "null";
        }

        if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
        {
            return $"{((DateTimeOffset)propertyValue).ToUniversalTime().ToString(_dateTimeFormat, CultureInfo.InvariantCulture)}";
        }

        return $"{propertyValue}";
    }
}
