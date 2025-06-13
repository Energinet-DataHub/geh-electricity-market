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
    private const int MaxDegreeOfParallelism = 20;
    private readonly DeltaLakeDataUploadStatementFormatter _deltaLakeDataUploadStatementFormatter = new();

    public async Task InsertParentElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingParentDto> electricalHeatingParent)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingParent);
        logger.LogInformation(
            "Starting upload of {Count} electrical heating parent metering points.", electricalHeatingParent.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingParentTableName}";

        var chunks = electricalHeatingParent.Chunk(51); // databricks max params (256) / number of properties
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Parents Uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task InsertChildElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingChildDto> electricalHeatingChildren)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingChildren);
        logger.LogInformation(
            "Starting upload of {Count} electrical heating child metering points.", electricalHeatingChildren.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingChildTableName}";

        var chunks = electricalHeatingChildren.Chunk(42); // databricks max params (256) / number of properties
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Children Uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task DeleteParentElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingEmptyDto> electricalHeatingEmpty)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingEmpty);
        logger.LogInformation(
            "Starting delete of {Count} electrical heating parent metering points.", electricalHeatingEmpty.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingParentTableName}";

        var chunks = electricalHeatingEmpty.Chunk(256); // databricks max params (256) / number of properties
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Parents Deleted: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task DeleteChildElectricalHeatingAsync(IReadOnlyList<ElectricalHeatingEmptyDto> electricalHeatingEmpty)
    {
        ArgumentNullException.ThrowIfNull(electricalHeatingEmpty);
        logger.LogInformation(
            "Starting delete of {Count} electrical heating child metering points.", electricalHeatingEmpty.Count);
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.ElectricalHeatingChildTableName}";

        var chunks = electricalHeatingEmpty.Chunk(256); // databricks max params (256) / number of properties
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Child Deleted: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task InsertCapacitySettlementPeriodsAsync(IReadOnlyList<CapacitySettlementPeriodDto> capacitySettlementPeriods, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capacitySettlementPeriods);
        logger.LogInformation(
            "Starting upload of {Count} capacity settlement metering point periods.", capacitySettlementPeriods.Count);
        const int chunkSize = 42; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.CapacitySettlementPeriodTableName}";

        var chunks = capacitySettlementPeriods.Chunk(chunkSize);
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = cancellationToken }, async (chunk, ctx) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, ctx);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Capacity settlement Uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task DeleteCapacitySettlementPeriodsAsync(IReadOnlyList<CapacitySettlementEmptyDto> capacitySettlementEmptyDtos, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capacitySettlementEmptyDtos);
        logger.LogInformation(
            "Starting clearing of {Count} capacity settlement metering point periods.", capacitySettlementEmptyDtos.Count);
        const int chunkSize = 256; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.CapacitySettlementPeriodTableName}";

        var chunks = capacitySettlementEmptyDtos.Chunk(chunkSize);
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = cancellationToken }, async (chunk, ctx) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, ctx);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Capacity settlement deleted: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task DeleteNetConsumptionParentsAsync(IReadOnlyList<NetConsumptionEmptyDto> netConsumptionEmptyDtos)
    {
        ArgumentNullException.ThrowIfNull(netConsumptionEmptyDtos);
        logger.LogInformation(
            "Starting delete of {Count} net consumption parent/child metering points.", netConsumptionEmptyDtos.Count);
        const int chunkSize = 250; // databricks max params (256) / number of properties
        var parentTableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.NetConsumptionParentTableName}";

        var chunks = netConsumptionEmptyDtos.Chunk(chunkSize);
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var parentDeleteStatement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(parentTableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(parentDeleteStatement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Net consumption parents deleted: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task InsertNetConsumptionParentsAsync(IReadOnlyList<NetConsumptionParentDto> netConsumptionParents)
    {
        ArgumentNullException.ThrowIfNull(netConsumptionParents);
        logger.LogInformation(
            "Starting upload of {Count} net consumption parent metering points.", netConsumptionParents.Count);
        const int chunkSize = 42; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.NetConsumptionParentTableName}";

        var chunks = netConsumptionParents.Chunk(chunkSize);
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Net Consumption Parents Uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task DeleteNetConsumptionChildrenAsync(IReadOnlyList<NetConsumptionEmptyDto> netConsumptionEmptyDtos)
    {
        ArgumentNullException.ThrowIfNull(netConsumptionEmptyDtos);
        logger.LogInformation(
            "Starting delete of {Count} net consumption parent/child metering points.", netConsumptionEmptyDtos.Count);
        const int chunkSize = 250; // databricks max params (256) / number of properties
        var childrenTableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.NetConsumptionChildTableName}";

        var chunks = netConsumptionEmptyDtos.Chunk(chunkSize);
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var childrenEmptyDto = chunk.Select(x => new { ParentMeteringPointId = x.MeteringPointId });
            var childrenDeleteStatement =
                _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(childrenTableName, childrenEmptyDto);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(childrenDeleteStatement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Net consumption children deleted: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task InsertNetConsumptionChildrenAsync(IReadOnlyList<NetConsumptionChildDto> netConsumptionChildren)
    {
        ArgumentNullException.ThrowIfNull(netConsumptionChildren);
        logger.LogInformation(
            "Starting upload of {Count} net consumption child metering points.", netConsumptionChildren.Count);
        const int chunkSize = 51; // databricks max params (256) / number of properties
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.NetConsumptionChildTableName}";
        var chunks = netConsumptionChildren.Chunk(chunkSize);
        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);

            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);
            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Net Consumption Children Uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task InsertHullerLogPeriodsAsync(IReadOnlyList<HullerLogDto> hullerLogs)
    {
        ArgumentNullException.ThrowIfNull(hullerLogs);

        logger.LogInformation(
            "Starting upload of {Count} huller log metering points.", hullerLogs.Count);

        const int chunkSize = 50;
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.MissingMeasurementLogsTableName}";

        var chunks = hullerLogs.Chunk(chunkSize);

        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);
            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);

            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Huller log uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task DeleteHullerLogPeriodsAsync(IReadOnlyList<HullerLogEmptyDto> emptyHullerLogs)
    {
        ArgumentNullException.ThrowIfNull(emptyHullerLogs);
        logger.LogInformation(
            "Starting clearing of {Count} huller log metering point periods.", emptyHullerLogs.Count);

        const int maxBatchSize = 250;

        var allResults = new List<object>();
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.MissingMeasurementLogsTableName}";

        var chunks = emptyHullerLogs.Chunk(maxBatchSize);

        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);
            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);

            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Parents Uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task InsertMeasurementsReportPeriodsAsync(IReadOnlyList<MeasurementsReportDto> measurementsReports)
    {
        ArgumentNullException.ThrowIfNull(measurementsReports);

        logger.LogInformation(
            "Starting upload of {Count} measurements report metering points.", measurementsReports.Count);

        const int chunkSize = 20;
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.MeasurementsReportTableName}";

        var chunks = measurementsReports.Chunk(chunkSize);

        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateInsertStatementWithParameters(tableName, chunk);
            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);

            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Measurements report uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }

    public async Task DeleteMeasurementsReportPeriodsAsync(IReadOnlyList<MeasurementsReportEmptyDto> emptyMeasurementsReports)
    {
        ArgumentNullException.ThrowIfNull(emptyMeasurementsReports);
        logger.LogInformation(
            "Starting clearing of {Count} measurements report metering point periods.", emptyMeasurementsReports.Count);

        const int maxBatchSize = 250;

        var allResults = new List<object>();
        var tableName = $"{catalogOptions.Value.Name}.{catalogOptions.Value.SchemaName}.{catalogOptions.Value.MeasurementsReportTableName}";

        var chunks = emptyMeasurementsReports.Chunk(maxBatchSize);

        await Parallel.ForEachAsync(chunks, new ParallelOptions() { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (chunk, cancellationToken) =>
        {
            var statement = _deltaLakeDataUploadStatementFormatter.CreateDeleteStatementWithParameters(tableName, chunk);
            var result = databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(statement, cancellationToken);

            await foreach (var record in result.ConfigureAwait(false))
            {
                string resultString = JsonSerializer.Serialize(record);
                logger.LogInformation(" - Electrical Heating Parents Uploaded: {ResultString}", resultString);
            }
        }).ConfigureAwait(false);
    }
}
