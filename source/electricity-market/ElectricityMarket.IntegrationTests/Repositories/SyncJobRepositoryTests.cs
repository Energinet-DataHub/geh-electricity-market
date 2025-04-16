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
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Repositories;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public class SyncJobRepositoryTests : IAsyncLifetime
{
    private readonly ElectricityMarketDbUpDatabaseFixture _fixture;

    public SyncJobRepositoryTests(ElectricityMarketDbUpDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetJob_WithNoData_ReturnsNotNull()
    {
        // Arrange
        var repository = new SyncJobRepository(_fixture.DbUpDatabaseManager.CreateDbContext());

        // Act
        var job = await repository.GetByNameAsync(SyncJobName.ElectricalHeating);

        // Assert
        Assert.NotNull(job);
    }

    [Fact]
    public async Task AddJob_ReturnsSuccess()
    {
        // Arrange
        var repository = new SyncJobRepository(_fixture.DbUpDatabaseManager.CreateDbContext());
        var job = new SyncJob(SyncJobName.ElectricalHeating, DateTimeOffset.UtcNow);

        // Act
        var result = await repository.AddOrUpdateAsync(job);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AddJob_CanRetrieve_ReturnCorrect()
    {
        // Arrange
        var repository = new SyncJobRepository(_fixture.DbUpDatabaseManager.CreateDbContext());
        var repository2 = new SyncJobRepository(_fixture.DbUpDatabaseManager.CreateDbContext());
        var job = new SyncJob(SyncJobName.ElectricalHeating, DateTimeOffset.UtcNow);

        // Act
        var result = await repository.AddOrUpdateAsync(job);
        var job2 = await repository2.GetByNameAsync(SyncJobName.ElectricalHeating);

        // Assert
        Assert.True(result);
        Assert.NotNull(job2);
        Assert.Equal(job.Name, job2.Name);
        Assert.Equal(job.Version, job2.Version);
    }

    [Fact]
    public async Task UpdateJob_CanRetrieve_ReturnCorrect()
    {
        // Arrange
        var repository = new SyncJobRepository(_fixture.DbUpDatabaseManager.CreateDbContext());
        var repository2 = new SyncJobRepository(_fixture.DbUpDatabaseManager.CreateDbContext());
        var repository3 = new SyncJobRepository(_fixture.DbUpDatabaseManager.CreateDbContext());
        var job = new SyncJob(SyncJobName.ElectricalHeating, DateTimeOffset.UtcNow);

        // Act
        var result = await repository.AddOrUpdateAsync(job);
        var job2 = await repository2.GetByNameAsync(SyncJobName.ElectricalHeating);
        var version = DateTimeOffset.UtcNow.AddDays(-1);
        job2 = job2 with { Version = version };
        await repository2.AddOrUpdateAsync(job2);
        var job3 = await repository3.GetByNameAsync(SyncJobName.ElectricalHeating);

        // Assert
        Assert.True(result);
        Assert.NotNull(job3);
        Assert.Equal(job.Name, job3.Name);
        Assert.Equal(version, job3.Version);
    }

    public async Task InitializeAsync()
    {
        var context = _fixture.DbUpDatabaseManager.CreateDbContext();
        await context.SyncJobs.ExecuteDeleteAsync();
    }

    public Task DisposeAsync()
    {
       return Task.CompletedTask;
    }
}
