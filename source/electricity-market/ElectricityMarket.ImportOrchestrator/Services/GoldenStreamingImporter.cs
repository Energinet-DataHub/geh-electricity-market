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

using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.EntityFrameworkCore;

namespace ElectricityMarket.ImportOrchestrator.Services;

internal sealed class GoldenStreamingImporter : IGoldenStreamingImporter
{
    private readonly ElectricityMarketDatabaseContext _databaseContext;
    private readonly IImportStateService _importStateService;
    private readonly IStreamingImporter _streamingImporter;

    public GoldenStreamingImporter(
        ElectricityMarketDatabaseContext databaseContext,
        IImportStateService importStateService,
        IStreamingImporter streamingImporter)
    {
        _databaseContext = databaseContext;
        _importStateService = importStateService;
        _streamingImporter = streamingImporter;
    }

    public async Task ImportAsync()
    {
        var previousCutoff = await _importStateService
            .GetStreamingImportCutoffAsync()
            .ConfigureAwait(false);

        var transaction = await _databaseContext.Database
            .BeginTransactionAsync()
            .ConfigureAwait(false);

        await using (transaction.ConfigureAwait(false))
        {
            var query = await _databaseContext
                .ImportedTransactions
                .Where(t => t.btd_trans_doss_id > previousCutoff)
                .OrderBy(t => t.btd_trans_doss_id)
                .ThenBy(t => t.metering_point_state_id)
                .ToListAsync()
                .ConfigureAwait(false);

            var nextCutoff = previousCutoff;

            foreach (var importedTransaction in query)
            {
                await _streamingImporter.ImportAsync(importedTransaction).ConfigureAwait(false);
                nextCutoff = importedTransaction.btd_trans_doss_id;
            }

            await _importStateService
                .UpdateStreamingCutoffAsync(nextCutoff)
                .ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
        }
    }
}
