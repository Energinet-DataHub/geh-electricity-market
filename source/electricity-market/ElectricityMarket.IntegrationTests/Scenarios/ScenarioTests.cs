﻿// Copyright 2020 Energinet DataHub A/S
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
using InMemImporter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Scenarios;

public class ScenarioTests
{
    public static IEnumerable<object[]> GetTestScenarios()
    {
        var files = typeof(ScenarioTests).Assembly.GetManifestResourceNames()
            .Where(x => x.Contains("IntegrationTests", StringComparison.OrdinalIgnoreCase) &&
                        x.Contains("TestData", StringComparison.OrdinalIgnoreCase) &&
                        x.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            yield return
            [
                Path.GetFileNameWithoutExtension(file).Replace("Energinet.DataHub.ElectricityMarket.IntegrationTests.TestData.", string.Empty, StringComparison.InvariantCultureIgnoreCase),
                file,
            ];
        }
    }

    [Theory]
    [MemberData(nameof(GetTestScenarios))]
    public async Task Test_Scenario(string name, string path)
    {
        // Arrange + Act
        var (success, message) = await RunScenariosWithinTransactionWithRollbackAsync(name, path);

        // Assert
        Assert.True(success, message);
    }

    private static async Task<(bool Success, string? Message)> RunScenariosWithinTransactionWithRollbackAsync(string name, string path)
    {
        var assembly = typeof(ScenarioTests).Assembly;
        await using var fixture = new ScenarioTestFixture();
        await fixture.InitializeAsync();

        using var scope = fixture.ServiceProvider.CreateScope();
        await using var context = fixture.DatabaseManager.CreateDbContext();

        await using var csvStream = assembly.GetManifestResourceStream(path);
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
        var expectedResults = assembly.GetManifestResourceStream(path.Replace(".csv", ".txt", StringComparison.OrdinalIgnoreCase));
        if (expectedResults is null)
        {
            return (false, $"No data found for {name}");
        }

        string expected;
        using (var reader = new StreamReader(expectedResults))
        {
            expected = Sanitize(await reader.ReadToEndAsync());
        }

        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase)
            ? (true, string.Empty)
            : (false, $"""
                       -----------------------------Generated-----------------------------
                       {actual}
                       -----------------------------Expected------------------------------
                       {expected}
                       """);
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
}
