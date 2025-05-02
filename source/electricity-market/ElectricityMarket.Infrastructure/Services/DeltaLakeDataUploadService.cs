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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class DeltaLakeDataUploadService : IDeltaLakeDataUploadService
{
    private readonly IOptions<DatabricksCatalogOptions> _catalogOptions;
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly DeltaLakeDataUploadStatementFormatter _deltaLakeDataUploadStatementFormatter = new();
    private readonly ILogger<DeltaLakeDataUploadService> _logger;

    public DeltaLakeDataUploadService(
        IOptions<DatabricksCatalogOptions> catalogOptions,
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        ILogger<DeltaLakeDataUploadService> logger)
    {
        _catalogOptions = catalogOptions;
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _logger = logger;
    }

    public async Task ImportTransactionsAsync(IEnumerable<ElectricalHeatingParentDto> electricalHeatingParent)
    {
        var tableName = $"{_catalogOptions.Value.Name}.{_catalogOptions.Value.SchemaName}.{_catalogOptions.Value.ElectricalHeatingParentTableName}";
        var queryString = _deltaLakeDataUploadStatementFormatter.CreateUploadStatement(tableName, electricalHeatingParent);
        var query = DatabricksStatement.FromRawSql(queryString);

        var result = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(query.Build());
        await foreach (var record in result.ConfigureAwait(false))
        {
            string resultString = Convert.ToString(record);
            _logger.LogInformation("Electrical Heating Parents Uploaded: {ResultString}", resultString);
        }
    }

    public async Task ImportTransactionsAsync(IEnumerable<ElectricalHeatingChildDto> electricalHeatingChildren)
    {
        var tableName = $"{_catalogOptions.Value.Name}.{_catalogOptions.Value.SchemaName}.{_catalogOptions.Value.ElectricalHeatingChildTableName}";
        var queryString = _deltaLakeDataUploadStatementFormatter.CreateUploadStatement(tableName, electricalHeatingChildren);
        var query = DatabricksStatement.FromRawSql(queryString);

        var result = _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(query.Build());
        await foreach (var record in result.ConfigureAwait(false))
        {
            string resultString = Convert.ToString(record);
            _logger.LogInformation("Electrical Heating Childrenn Uploaded: {ResultString}", resultString);
        }
    }
}
