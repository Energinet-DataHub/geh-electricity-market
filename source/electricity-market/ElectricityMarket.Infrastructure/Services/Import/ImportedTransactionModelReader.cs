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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class ImportedTransactionModelReader : IImportedTransactionModelReader
{
    private readonly IDbContextFactory<ElectricityMarketDatabaseContext> _contextFactory;
    private readonly ILogger _logger;

    public ImportedTransactionModelReader(
        IDbContextFactory<ElectricityMarketDatabaseContext> contextFactory,
        ILogger<ImportedTransactionModelReader> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task ReadImportedTransactionsAsync(int skip, int take, BlockingCollection<IList<ImportedTransactionEntity>> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var readContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        readContext.Database.SetCommandTimeout(60 * 5);

        await using (readContext.ConfigureAwait(false))
        {
            var meteringPointsQuery = readContext.ImportedTransactions
                .AsNoTracking()
                .Select(t => t.metering_point_id)
                .Distinct()
                .OrderBy(meteringPointId => meteringPointId)
                .Skip(skip)
                .Take(take);

            var importedTransactionsStream = readContext.ImportedTransactions
                .AsNoTracking()
                .Join(
                    meteringPointsQuery,
                    outer => outer.metering_point_id,
                    inner => inner,
                    (entity, _) => entity)
                .OrderBy(t => t.metering_point_id)
                .ThenBy(t => t.btd_trans_doss_id)
                .ThenBy(t => t.metering_point_state_id)
                .AsAsyncEnumerable();

            var transactionsForOneMp = new List<ImportedTransactionEntity>();

            await foreach (var importedTransaction in importedTransactionsStream.ConfigureAwait(false))
            {
                if (transactionsForOneMp.Count > 0 &&
                    transactionsForOneMp[0].metering_point_id != importedTransaction.metering_point_id)
                {
                    result.Add(transactionsForOneMp);
                    transactionsForOneMp = new List<ImportedTransactionEntity>();
                }

                transactionsForOneMp.Add(importedTransaction);
            }

            if (transactionsForOneMp.Count != 0)
            {
                result.Add(transactionsForOneMp);
            }

            result.CompleteAdding();
            _logger.LogWarning("All imported transactions for range {BeginRange} were read.", skip);
        }
    }
}
