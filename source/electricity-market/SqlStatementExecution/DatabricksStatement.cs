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

namespace Energinet.DataHub.Core.DatabricksExperimental.SqlStatementExecution;

/// <summary>
/// A statement to be executed on Databricks SQL Warehouse
/// </summary>
public abstract class DatabricksStatement
{
    /// <summary>
    /// Create an adhoc SQL statement with optional parameters
    /// </summary>
    /// <param name="sqlStatement">Query to execute on Databricks SQL Warehouse</param>
    /// <returns><see cref="DatabricksStatementBuilder"/> for customizing parameters</returns>
    public static DatabricksStatementBuilder FromRawSql(string sqlStatement) => new DatabricksStatementBuilder(sqlStatement);

    public override string ToString() => GetSqlStatement();

    /// <summary>
    /// Get the SQL statement
    /// </summary>
    /// <returns>SQL statement to be executed</returns>
    protected internal abstract string GetSqlStatement();

    /// <summary>
    /// When overridden in a derived class, returns the parameters for the SQL statement
    /// </summary>
    /// <returns>Parameters for the SQL statement</returns>
    protected internal virtual IReadOnlyCollection<QueryParameter> GetParameters() => Array.Empty<QueryParameter>();
}
