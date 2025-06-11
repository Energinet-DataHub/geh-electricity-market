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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public class DeltaLakeDataUploadService(
    IOptions<DatabricksCatalogOptions> catalogOptions,
    DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
    ILogger<DeltaLakeDataUploadService> logger)
    : IDeltaLakeDataUploadService
{
    private readonly DeltaLakeDataUploadStatementFormatter _deltaLakeDataUploadStatementFormatter = new();

    public async Task InsertParentElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingParentDto> electricalHeatingParent)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingParent);
        logger.LogInformation(
            "Starting upload of {Count} electrical heating parent metering points.", electricalHeatingParent.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingParentTableName}";

        var chunks = electricalHeatingParent.Chunk(51); // databricks max params (256) / number of properties
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Parents Uploaded: {ResultString}", resultString);
            }
        }
    }

    public async Task InsertChildElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingChildDto> electricalHeatingChildren)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingChildren);
        logger.LogInformation(
            "Starting upload of {Count} electrical heating child metering points.", electricalHeatingChildren.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingChildTableName}";

        var chunks = electricalHeatingChildren.Chunk(42); // databricks max params (256) / number of properties
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Children Uploaded: {ResultString}", resultString);
            }
        }
    }

    public async Task DeleteParentElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingEmptyDto> electricalHeatingEmpty)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingEmpty);
        logger.LogInformation(
            "Starting delete of {Count} electrical heating parent metering points.", electricalHeatingEmpty.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingParentTableName}";

        var chunks = electricalHeatingEmpty.Chunk(256); // databricks max params (256) / number of properties
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Parents Deleted: {ResultString}", resultString);
            }
        }
    }

    public async Task DeleteChildElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingEmptyDto> electricalHeatingEmpty)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingEmpty);
        logger.LogInformation(
            "Starting delete of {Count} electrical heating child metering points.", electricalHeatingEmpty.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingChildTableName}";

        var chunks = electricalHeatingEmpty.Chunk(256); // databricks max params (256) / number of properties
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Child Deleted: {ResultString}", resultString);
            }
        }
    }

    public async Task InsertCapacitySettlementPeriodsAsync(IReadOnlyList<CapacitySettlementPeriodDto> capacitySettlementPeriods, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capacitySettlementPeriods);
        logger.LogInformation(
            "Starting upload of {Count} capacity settlement metering point periods.", capacitySettlementPeriods.Count);
        const int chunkSize = 42; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.CapacitySettlementPeriodTableName}";

        var chunks = capacitySettlementPeriods.Chunk(chunkSize);
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Capacity settlement Uploaded: {ResultString}", resultString);
            }
        }
    }

    public async Task DeleteCapacitySettlementPeriodsAsync(IReadOnlyList<CapacitySettlementEmptyDto> capacitySettlementEmptyDtos, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capacitySettlementEmptyDtos);
        logger.LogInformation(
            "Starting clearing of {Count} capacity settlement metering point periods.", capacitySettlementEmptyDtos.Count);
        const int chunkSize = 256; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.CapacitySettlementPeriodTableName}";

        var chunks = capacitySettlementEmptyDtos.Chunk(chunkSize);
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Capacity settlement deleted: {ResultString}", resultString);
            }
        }
    }

    public async Task ImportTransactionsAsync(IReadOnlyList<NetConsumptionParentDto> netConsumptionParents)
    {
        ArgumentNullException.ThrowIfNull(netConsumptionParents);
        logger.LogInformation(
            "Starting upload of {Count} net consumption parent metering points.", netConsumptionParents.Count);
        const int chunkSize = 42; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.NetConsumptionParentTableName}";

        var chunks = netConsumptionParents.Chunk(chunkSize);
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateUploadStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Net Consumption Parents Uploaded: {ResultString}", resultString);
            }
        }
    }

    public async Task ImportTransactionsAsync(IReadOnlyList<NetConsumptionChildDto> netConsumptionChildren)
    {
        ArgumentNullException.ThrowIfNull(netConsumptionChildren);
        logger.LogInformation(
            "Starting upload of {Count} net consumption child metering points.", netConsumptionChildren.Count);
        const int chunkSize = 51; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.NetConsumptionChildTableName}";
        var chunks = netConsumptionChildren.Chunk(chunkSize);
        foreach (var chunk in chunks)
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateUploadStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Net Consumption Children Uploaded: {ResultString}", resultString);
            }
        }
    }

    public async Task InsertHullerLogPeriodsAsync(IReadOnlyList<HullerLogDto> hullerLogs)
    {
        ArgumentNullException.ThrowIfNull(hullerLogs);

        logger.LogInformation(
            "Starting upload of {Count} huller log metering points.", hullerLogs.Count);

        const int chunkSize = 50;
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.MissingMeasurementLogsTableName}";

        foreach (var batch in hullerLogs.Chunk(chunkSize))
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, batch);
            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);

            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Huller log uploaded: {ResultString}", resultString);
            }
        }
    }

    public async Task DeleteHullerLogPeriodsAsync(IReadOnlyList<HullerLogEmptyDto> emptyHullerLogs)
    {
        ArgumentNullException.ThrowIfNull(emptyHullerLogs);
        logger.LogInformation(
            "Starting clearing of {Count} huller log metering point periods.", emptyHullerLogs.Count);

        const int maxBatchSize = 250;

        var allResults = new List<object>();
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.MissingMeasurementLogsTableName}";

        foreach (var batch in emptyHullerLogs.Chunk(maxBatchSize))
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, batch);
            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement);

            await foreach (var record in result.ConfigureAwait(false))
            {
                allResults.Add(record);
            }
        }

        foreach (var record in allResults)
        {
            string resultString = JsonSerializer.Serialize(record);
            logger.LogInformation("Huller log deleted: {ResultString}", resultString);
        }
    }
}
