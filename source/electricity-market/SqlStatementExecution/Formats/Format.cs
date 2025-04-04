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

namespace Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.Formats;

public class Format
{
    private const string JsonFormat = nameof(JsonFormat);
    private const string ArrowFormat = nameof(ArrowFormat);

    public static Format JsonArray => new(JsonFormat);

    public static Format ApacheArrow => new(ArrowFormat);

    private Format(string format) => Value = format;

    public string Value { get; }

    internal IExecuteStrategy GetStrategy(DatabricksSqlStatementOptions options) => Value switch
    {
        JsonFormat => new JsonArrayFormat(options),
        ArrowFormat => new ApacheArrowFormat(options),
        _ => throw new ArgumentException($"Unknown format: {Value}"),
    };
}
