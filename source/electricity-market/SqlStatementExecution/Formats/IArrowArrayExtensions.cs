﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Dynamic;
using Apache.Arrow;
using Apache.Arrow.Types;

namespace Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.Formats;

internal static class IArrowArrayExtensions
{
    public static object? GetValue(this IArrowArray arrowArray, int i)
        => arrowArray switch
        {
            BooleanArray booleanArray => booleanArray.GetValue(i),
            Int8Array int8Array => int8Array.GetValue(i),
            Int16Array int16Array => int16Array.GetValue(i),
            Int32Array int32Array => int32Array.GetValue(i),
            Int64Array int64Array => int64Array.GetValue(i),
            UInt8Array uint8Array => uint8Array.GetValue(i),
            UInt16Array uint16Array => uint16Array.GetValue(i),
            UInt32Array uint32Array => uint32Array.GetValue(i),
            UInt64Array uint64Array => uint64Array.GetValue(i),
            FloatArray floatArray => floatArray.GetValue(i),
            DoubleArray doubleArray => doubleArray.GetValue(i),
            Date32Array date32Array => date32Array.GetValue(i),
            Date64Array date64Array => date64Array.GetValue(i),
            TimestampArray timestampArray => timestampArray.GetTimestamp(i),
            Decimal128Array decimal128Array => decimal128Array.GetValue(i),
            StringArray stringArray => stringArray.GetString(i),
            ListArray listArray => ReadArray(listArray, i),
            StructArray structArray => ReadStructArray(structArray, i),
            _ => throw new NotSupportedException($"Unsupported data type {arrowArray}"),
        };

    private static object? ReadArray(ListArray array, int i)
    {
        var slice = array.GetSlicedValues(i);
        var objectArray = new object?[slice.Length];

        for (var j = 0; j < objectArray.Length; j++)
        {
            objectArray[j] = slice.GetValue(j);
        }

        return objectArray;
    }

    private static object? ReadStructArray(StructArray array, int i)
    {
        if (array.Data.DataType is not StructType structType)
            return null;

        var structObject = new ExpandoObject();
        for (var k = 0; k < structType.Fields.Count; k++)
        {
            var field = structType.Fields[k];
            var value = array.Fields[k].GetValue(i);
            ((IDictionary<string, object?>)structObject).Add(field.Name, value);
        }

        return structObject;
    }
}
