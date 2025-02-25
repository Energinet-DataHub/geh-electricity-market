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
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class ImportedTransactionRepository : IImportedTransactionRepository
{
    private readonly ElectricityMarketDatabaseContext _context;

    public ImportedTransactionRepository(ElectricityMarketDatabaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(IEnumerable<ImportedTransactionRecord> transactions)
    {
        var mapped = transactions.Select(result => new ImportedTransactionEntity
        {
            metering_point_id = result.MeteringPointId,
            valid_from_date = result.ValidFromDate,
            valid_to_date = result.ValidToDate ?? DateTimeOffset.MaxValue,
            dh2_created = result.DH2CreatedDate,
            metering_grid_area_id = result.MeteringGridAreaId,
            metering_point_state_id = result.MeteringPointStateId,
            btd_trans_doss_id = result.BusinessTransactionDosId ?? -1,
            physical_status_of_mp = result.PhysicalStatus,
            type_of_mp = result.Type,
            sub_type_of_mp = result.SubType,
            energy_timeseries_measure_unit = result.MeasureUnit,
            web_access_code = result.WebAccessCode,
            balance_supplier_id = result.BalanceSupplierId,
            effectuation_date = result.EffectuationDate ?? result.DH2CreatedDate,
            transaction_type = result.TransactionType ?? string.Empty,
        });

        await _context.ImportedTransactions
            .AddRangeAsync(mapped)
            .ConfigureAwait(false);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<long>> GetImportedTransactionsAsync(IEnumerable<ImportedTransactionRecord> transactions)
    {
        var ids = transactions
            .Select(id => id.MeteringPointId)
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
}
