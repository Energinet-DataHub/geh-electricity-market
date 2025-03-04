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

using System;
using System.Globalization;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class StreamingImporter : IStreamingImporter
{
    private readonly ElectricityMarketDatabaseContext _databaseContext;
    private readonly IMeteringPointImporter _meteringPointImporter;

    public StreamingImporter(
        ElectricityMarketDatabaseContext databaseContext,
        IMeteringPointImporter meteringPointImporter)
    {
        _databaseContext = databaseContext;
        _meteringPointImporter = meteringPointImporter;
    }

    public async Task ImportAsync(ImportedTransactionEntity importedTransactionEntity)
    {
        ArgumentNullException.ThrowIfNull(importedTransactionEntity);

        await _databaseContext.ImportedTransactions
            .AddAsync(importedTransactionEntity)
            .ConfigureAwait(false);

        var existingMeteringPoint = await _databaseContext.MeteringPoints
            .FindAsync(importedTransactionEntity.metering_point_id)
            .ConfigureAwait(false);

        var meteringPointToUpdate = existingMeteringPoint ?? new MeteringPointEntity();

        var (imported, message) = await _meteringPointImporter
            .ImportAsync(meteringPointToUpdate, [importedTransactionEntity])
            .ConfigureAwait(false);

        if (imported)
        {
            if (existingMeteringPoint == null)
                await _databaseContext.MeteringPoints.AddAsync(meteringPointToUpdate).ConfigureAwait(false);
        }
        else
        {
            _databaseContext.Entry(meteringPointToUpdate).State = EntityState.Detached;

            await _databaseContext.QuarantinedMeteringPointEntities
                .AddAsync(new QuarantinedMeteringPointEntity
                {
                    Identification = importedTransactionEntity.metering_point_id.ToString(CultureInfo.InvariantCulture),
                    Message = message
                })
                .ConfigureAwait(false);
        }

        await _databaseContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
