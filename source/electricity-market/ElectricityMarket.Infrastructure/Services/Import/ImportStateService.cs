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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class ImportStateService : IImportStateService
{
    private readonly ElectricityMarketDatabaseContext _databaseContext;

    public ImportStateService(ElectricityMarketDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public Task<bool> IsImportPendingAsync()
    {
        return _databaseContext.ImportStates.AnyAsync(state => state.State == ImportStateEntity.Scheduled);
    }

    public Task<bool> IsStreamingImportEnabledAsync()
    {
        return _databaseContext.ImportStates.AnyAsync(state => state.State == ImportStateEntity.StreamingImport);
    }

    public Task<bool> ShouldStreamFromGoldAsync()
    {
        return _databaseContext.ImportStates.AnyAsync(state => state.State == ImportStateEntity.GoldenStreaming);
    }

    public async Task EnableBulkImportAsync()
    {
        var importState = await _databaseContext.ImportStates.SingleAsync().ConfigureAwait(false);
        importState.State = ImportStateEntity.BulkImport;
        await _databaseContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task EnableStreamingImportAsync(long cutoff)
    {
        var importState = await _databaseContext.ImportStates.SingleAsync().ConfigureAwait(false);
        importState.Offset = cutoff;
        importState.State = ImportStateEntity.StreamingImport;
        await _databaseContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public Task<long> GetStreamingImportCutoffAsync()
    {
        return _databaseContext.ImportStates.Select(state => state.Offset).SingleAsync();
    }

    public async Task UpdateStreamingCutoffAsync(long cutoff)
    {
        var importState = await _databaseContext.ImportStates.SingleAsync().ConfigureAwait(false);
        importState.Offset = cutoff;
        await _databaseContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
