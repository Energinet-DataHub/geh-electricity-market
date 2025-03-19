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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Options;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Energinet.DataHub.ElectricityMarket.IntegrationTests.Fixtures;
using FakeTimeZone;
using InMemImporter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios;

public class ScenarioTests
{
    [Fact]
    public async Task Run_All_Scenarios_And_Verify_Results()
    {
        // Arrange + Act
        var results = await RunScenariosWithinTransactionWithRollbackAsync();

        // Assert
        Assert.All(results, result => Assert.True(result.Success));
    }

    private static async Task<IEnumerable<ScenarioTestResult>> RunScenariosWithinTransactionWithRollbackAsync()
    {
        var scenarioResults = new List<ScenarioTestResult>();
        using (new FakeLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time")))
        {
            var assembly = typeof(ScenarioTests).Assembly;

            // Load all scenario files
            var scenarioFiles = assembly.GetManifestResourceNames().Where(IsScenarioTestFile);
            foreach (var scenarioFile in scenarioFiles)
            {
                await using var fixture = new ScenarioTestFixture();
                await fixture.InitializeAsync();

                using var scope = fixture.ServiceProvider.CreateScope();
                await using var context = fixture.DatabaseManager.CreateDbContext();

                // Import the test data
                var scenarioName = scenarioFile.Replace(
                    "Energinet.DataHub.ElectricityMarket.IntegrationTests.TestData.",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase);

                await using var csvStream = assembly.GetManifestResourceStream(scenarioFile);
                using var streamReader = new StreamReader(csvStream!);
                var rawCsv = await streamReader.ReadToEndAsync();
                var (cultureInfo, csv) = InMemCsvHelper.PreapareCsv(rawCsv);

                using var bulkImporter = new BulkImporter(
                    NullLogger<BulkImporter>.Instance,
                    new CsvImportedTransactionModelReader(csv, cultureInfo),
                    new RelationalModelWriter(scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>(), NullLogger<RelationalModelWriter>.Instance),
                    new MeteringPointImporter());

                await bulkImporter.RunAsync(0, int.MaxValue).ConfigureAwait(false);

                // Read the results and pretty print them
                var meteringPointEntities = await context.MeteringPoints.ToListAsync();
                var quarantinedEntities = await context.QuarantinedMeteringPointEntities.ToListAsync();
                var actual = Sanitize(await new RelationalModelPrinter().PrintAsync(
                    [meteringPointEntities],
                    [quarantinedEntities],
                    cultureInfo,
                    html: false));

                // Compare the results with the expected results
                var expectedResults = assembly.GetManifestResourceStream(scenarioFile.Replace(".csv", ".txt", StringComparison.OrdinalIgnoreCase));
                if (expectedResults is null)
                {
                    scenarioResults.Add(new ScenarioTestResult(scenarioName, false, "Expected results file not found"));
                    continue;
                }

                string expected;
                using (var reader = new StreamReader(expectedResults))
                {
                    expected = Sanitize(await reader.ReadToEndAsync());
                }

                scenarioResults.Add(actual.Equals(expected, StringComparison.OrdinalIgnoreCase)
                    ? new ScenarioTestResult(scenarioName, true, string.Empty)
                    : new ScenarioTestResult(scenarioName, false, "Results does not match expected\n-----------------------------\nGenerated\n-----------------------------\n" + actual + "\n-----------------------------\nExpected\n-----------------------------\n" + expected));
            }
        }

        return scenarioResults;
    }

    private static string Sanitize(string csv)
    {
        csv = csv.Replace("\r", string.Empty, StringComparison.InvariantCultureIgnoreCase);

        var lines = csv.Split('\n').ToList();
        var sanitizedLines = new List<string>();

        var commercialRelationIndex = lines.IndexOf("CommercialRelation");
        if (commercialRelationIndex == -1)
        {
            return csv;
        }

        var dataStartIndex = commercialRelationIndex + 4;

        var energySupplyPeriodIndex = lines.IndexOf("EnergySupplyPeriod");
        var dataStopIndex = energySupplyPeriodIndex - 2;

        var indexOfClientId = lines[commercialRelationIndex + 2].IndexOf("ClientId", StringComparison.InvariantCultureIgnoreCase);

        sanitizedLines.AddRange(lines[..dataStartIndex]);
        sanitizedLines.AddRange(lines[dataStartIndex..dataStopIndex].Select(x => x.Substring(0, indexOfClientId) + Guid.Empty + x.Substring(indexOfClientId + 36)));
        sanitizedLines.AddRange(lines[dataStopIndex..]);

        return string.Join(Environment.NewLine, sanitizedLines);
    }

    private static bool IsScenarioTestFile(string resourceName)
    {
        return resourceName.Contains("IntegrationTests", StringComparison.OrdinalIgnoreCase)
               && resourceName.Contains("TestData", StringComparison.OrdinalIgnoreCase)
               && resourceName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
    }
}
