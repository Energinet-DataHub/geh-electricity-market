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
using System.Runtime.CompilerServices;
using Apache.Arrow.Ipc;
using Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.Statement;

namespace Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.Formats;

internal class ApacheArrowFormat : IExecuteStrategy
{
    private readonly DatabricksSqlStatementOptions _options;

    public ApacheArrowFormat(DatabricksSqlStatementOptions options)
    {
        _options = options;
    }

    public DatabricksStatementRequest GetStatementRequest(DatabricksStatement statement)
        => new(_options.WarehouseId, statement, DatabricksStatementRequest.ArrowFormat);

    public async IAsyncEnumerable<dynamic> ExecuteAsync(Stream content, DatabricksStatementResponse response, [EnumeratorCancellation]CancellationToken cancellationToken)
    {
        using var reader = new ArrowStreamReader(content);

        var batch = await reader.ReadNextRecordBatchAsync(cancellationToken).ConfigureAwait(false);
        while (batch != null)
        {
            for (var i = 0; i < batch.Length; i++)
            {
                IDictionary<string, object?> record = new ExpandoObject();

                for (var c = 0; c < batch.ColumnCount; c++)
                {
                    var field = batch.Schema.GetFieldByIndex(c);
                    var column = batch.Column(c);
                    record[field.Name] = column.GetValue(i) ?? default;
                }

                yield return record;
            }

            batch = await reader.ReadNextRecordBatchAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
