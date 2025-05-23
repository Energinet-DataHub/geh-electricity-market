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

using Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution.Statement;

namespace Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution;

/// <summary>
/// IExecuteStrategy
/// </summary>
internal interface IExecuteStrategy
{
    /// <summary>
    /// GetStatementRequest
    /// </summary>
    /// <param name="statement"></param>
    /// <returns><see cref="DatabricksStatementRequest"/></returns>
    DatabricksStatementRequest GetStatementRequest(DatabricksStatement statement);

    /// <summary>
    /// ExecuteAsync
    /// </summary>
    /// <param name="content"></param>
    /// <param name="response"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="IAsyncEnumerable{T}"/></returns>
    IAsyncEnumerable<dynamic> ExecuteAsync(
        Stream content,
        DatabricksStatementResponse response,
        CancellationToken cancellationToken);
}
