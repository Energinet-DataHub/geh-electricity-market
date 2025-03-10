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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using FakeTimeZone;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios;

[Collection(nameof(IntegrationTestCollectionFixture))]
public class ScenarioTests : IClassFixture<ElectricityMarketDatabaseFixture>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICsvImporter _csvImporter;

    public ScenarioTests(ScenarioTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _csvImporter = fixture.ServiceProvider.GetRequiredService<ICsvImporter>();
        _serviceProvider = fixture.ServiceProvider;
    }

    [Fact(Skip = "Model not stable enough yet, once it's stable re-enable test")]
    public async Task Run_All_Scenarios_And_Verify_Results()
    {
        // Arrange + Act
        var results = await RunScenariosWithinTransactionWithRollbackAsync();

        // Assert
        Assert.All(results, result => Assert.True(result.Success));
    }

    private static string Sanitize(string input)
    {
        return input
            .Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("\r", "\n", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IEnumerable<ScenarioTestResult>> RunScenariosWithinTransactionWithRollbackAsync()
    {
        var scenarioResults = new List<ScenarioTestResult>();
        using (new FakeLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time")))
        {
            var assembly = typeof(ScenarioTests).Assembly;

            // Load all scenario files
            var scenarioFiles = assembly!.GetManifestResourceNames().Where(IsScenarioTestFile);
            foreach (var scenarioFile in scenarioFiles)
            {
                using var scope = _serviceProvider.CreateScope();

                // Setup needed services
                var scopedProvider = scope.ServiceProvider;
                var importedTransactionRepository = scopedProvider.GetRequiredService<IImportedTransactionRepository>();
                var importer = scopedProvider.GetRequiredService<IStreamingImporter>();
                var relationalModelPrinter = scopedProvider.GetRequiredService<IRelationalModelPrinter>();
                var contextFactory = scopedProvider.GetService<IDbContextFactory<ElectricityMarketDatabaseContext>>();
                await using var context = await contextFactory.CreateDbContextAsync();
                await using var dbTransaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Import the test data
                    var scenarioName = scenarioFile.Replace(
                        "Energinet.DataHub.ElectricityMarket.IntegrationTests.TestData.",
                        string.Empty,
                        StringComparison.OrdinalIgnoreCase);

                    var csvStream = assembly!.GetManifestResourceStream(scenarioFile);
                    var imported = await _csvImporter.ImportAsync(csvStream).ConfigureAwait(false);
                    await importedTransactionRepository.AddAsync(imported).ConfigureAwait(false);
                    var transactions = await context.ImportedTransactions
                        .ToListAsync()
                        .ConfigureAwait(false);

                    foreach (var transaction in transactions)
                    {
                        await importer.ImportAsync(transaction);
                    }

                    // Read the results and pretty print them
                    var meteringPointEntities = await context.MeteringPoints.ToListAsync();
                    var quarantinedEntities = await context.QuarantinedMeteringPointEntities.ToListAsync();
                    var prettyPrintedResult = await relationalModelPrinter.PrintAsync(
                        [meteringPointEntities],
                        [quarantinedEntities],
                        CultureInfo.GetCultureInfo("da-dk"));

                    prettyPrintedResult = Sanitize(prettyPrintedResult);

                    // Compare the results with the expected results
                    var expectedResults = assembly!.GetManifestResourceStream(scenarioFile.Replace(".csv", ".txt", StringComparison.OrdinalIgnoreCase));
                    if (expectedResults is null)
                    {
                        scenarioResults.Add(new ScenarioTestResult(scenarioName, false, "Expected results file not found"));
                        continue;
                    }

                    string expected;
                    using (var reader = new StreamReader(expectedResults!))
                    {
                        expected = Sanitize(await reader.ReadToEndAsync());
                    }

                    await dbTransaction.RollbackAsync();
                    scenarioResults.Add(prettyPrintedResult.Equals(expected, StringComparison.OrdinalIgnoreCase)
                        ? new ScenarioTestResult(scenarioName, true, string.Empty)
                        : new ScenarioTestResult(scenarioName, false, "Results does not match expected\n-----------------------------\nGenerated\n-----------------------------" + prettyPrintedResult + "\n-----------------------------\nExpected\n-----------------------------" + expected));
                }
                catch (Exception e)
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
        }

        return scenarioResults;
    }

    private bool IsScenarioTestFile(string resourceName)
    {
        return resourceName.Contains("IntegrationTests", StringComparison.OrdinalIgnoreCase)
               && resourceName.Contains("TestData", StringComparison.OrdinalIgnoreCase)
               && resourceName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
    }
}
