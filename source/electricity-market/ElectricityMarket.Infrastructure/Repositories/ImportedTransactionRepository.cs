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
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Helpers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class ImportedTransactionRepository : IImportedTransactionRepository
{
    private readonly ElectricityMarketDatabaseContext _context;
    private readonly IEnumerable<PropertyInfo> _importedTransactionProperties = [];
    private readonly Dictionary<string, Func<ImportedTransactionRecord, object>> _propertyGetters = [];

    public ImportedTransactionRepository(ElectricityMarketDatabaseContext context)
    {
        _context = context;
        _importedTransactionProperties = typeof(ImportedTransactionRecord).GetProperties().Where(p => p.CanRead);
        foreach (var propertyInfo in _importedTransactionProperties)
        {
            var getter = propertyInfo.GetValueGetter<ImportedTransactionRecord>();
            _propertyGetters.Add(propertyInfo.Name, getter);
        }
    }

    public async Task AddAsync(IEnumerable<ImportedTransactionRecord> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        var mapped = Map(transactions);

        await _context.ImportedTransactions
            .AddRangeAsync(mapped)
            .ConfigureAwait(false);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<long>> GetImportedTransactionsAsync(IEnumerable<ImportedTransactionRecord> transactions)
    {
        var ids = transactions
            .Select(id => id.metering_point_id)
            .Distinct()
            .ToList();

        var query =
            from it in _context.ImportedTransactions
            where ids.Contains(it.metering_point_id)
            select it.metering_point_id;

        var importedTransactionMeteringPointIds = await query
            .ToListAsync()
            .ConfigureAwait(false);

        return importedTransactionMeteringPointIds;
    }

    private IEnumerable<ImportedTransactionEntity> Map(IEnumerable<ImportedTransactionRecord> transactions)
    {
        var lookup = ImportModelHelper.ImportFields.ToImmutableDictionary(k => k.Key, v => v.Value);

        foreach (var transaction in transactions)
        {
            var entity = new ImportedTransactionEntity { Id = Guid.NewGuid() };

            foreach (var propertyInfo in _importedTransactionProperties)
            {
                var getter = _propertyGetters[propertyInfo.Name];
                var propertyValue = getter(transaction);
                lookup[propertyInfo.Name](propertyValue, entity);
            }

            if (!string.IsNullOrWhiteSpace(entity.first_consumer_cpr))
            {
                entity.first_consumer_cpr = "0123456789";
            }

            if (!string.IsNullOrWhiteSpace(entity.second_consumer_cpr))
            {
                entity.second_consumer_cpr = "0123456789";
            }

            yield return entity;
        }
    }
}
