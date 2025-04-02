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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;

public sealed class SyncJobRepository : ISyncJobsRepository
{
    private readonly ElectricityMarketDatabaseContext _context;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SyncJobRepository(ElectricityMarketDatabaseContext context)
    {
        _context = context;
        _jsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public async Task<bool> AddOrUpdateAsync(SyncJob job)
    {
        ArgumentNullException.ThrowIfNull(job, nameof(job));

        var entity = await _context
            .SyncJobs
            .FindAsync(job.Name)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new SyncJobsEntity
            {
                JobName = job.Name,
                Version = job.Version,
            };
            await _context.SyncJobs.AddAsync(entity).ConfigureAwait(false);
        }
        else
        {
            entity.Version = job.Version;
            _context.SyncJobs.Update(entity);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<SyncJob> GetByNameAsync(SyncJobName job)
    {
        var syncJob = await (
                from sync in _context.SyncJobs
                where sync.JobName == job
                select new SyncJob(sync.JobName, sync.Version))
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);
        return syncJob ?? new SyncJob(job, 0);
    }
}
