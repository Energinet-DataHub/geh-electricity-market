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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using FakeTimeZone;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios;

public class ScenarioBase : IClassFixture<ElectricityMarketDatabaseFixture>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICsvImporter _csvImporter;
    private readonly IBulkImporter _importer;
    private readonly IImportedTransactionRepository _importedTransactionRepository;

    public ScenarioBase(ScenarioTestFixture fixture)
    {
        _csvImporter = fixture.ServiceProvider.GetRequiredService<ICsvImporter>();
        _importedTransactionRepository = fixture.ServiceProvider.GetRequiredService<IImportedTransactionRepository>();
        _importer = fixture.ServiceProvider.GetRequiredService<IBulkImporter>();
        _serviceProvider = fixture.ServiceProvider;
    }

    protected async ValueTask RunWithinTransactionWithRollbackAsync(Action action, string csvResourceName)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(csvResourceName);
        await RunWithinTransactionWithRollbackAsync(action, csvResourceName, TransactionManager.DefaultTimeout).ConfigureAwait(false);
    }

    protected async ValueTask RunWithinTransactionWithRollbackMaxTimeoutAsync(Action action, string csvResourceName)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(csvResourceName);
        await RunWithinTransactionWithRollbackAsync(action, csvResourceName, TransactionManager.MaximumTimeout).ConfigureAwait(false);
    }

    protected T GetService<T>()
        where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    private async ValueTask RunWithinTransactionWithRollbackAsync(Action action, string csvResourceName, TimeSpan timeout, string timeZoneId = "Romance Standard Time")
    {
        using (new FakeLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)))
        {
            var options = default(TransactionOptions);
            var assembly = typeof(ScenarioBase).Assembly;
            var resourceName = assembly!.GetManifestResourceNames().Single(n => n.EndsWith(csvResourceName, StringComparison.OrdinalIgnoreCase));
            var csvStream = assembly!.GetManifestResourceStream(resourceName);
            options.IsolationLevel = IsolationLevel.ReadCommitted;
            options.Timeout = timeout;
            using (new TransactionScope(TransactionScopeOption.Required, options, TransactionScopeAsyncFlowOption.Enabled))
            {
                await ImportCsv(csvStream!).ConfigureAwait(false);
                action();
            }
        }
    }

    private async ValueTask ImportCsv(Stream inputStream, int maxImportsSize = 10_000) // 10_000 selected as a max size, test data should never exceed this.
    {
        var imported = await _csvImporter.ImportAsync(inputStream).ConfigureAwait(false);
        await _importedTransactionRepository.AddAsync(imported).ConfigureAwait(false);
        await _importer.RunAsync(0, maxImportsSize);
    }
}
