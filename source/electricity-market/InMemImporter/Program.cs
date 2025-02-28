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

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace InMemImporter;

public static class Program
{
    public static async Task Main(params string[] args)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(args);

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var cultureInfo = CultureInfo.GetCultureInfo(config["Locale"]!);

            using var importer = new Importer(
                NullLogger<Importer>.Instance,
                new CsvImportedTransactionModelReader(args[0], cultureInfo),
                new StdOutRelationalModelWriter(cultureInfo),
                new MeteringPointImporter());

            await importer.RunAsync(0, int.MaxValue).ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Console.WriteLine(e);
        }
    }

    private sealed class CsvImportedTransactionModelReader : IImportedTransactionModelReader
    {
        private readonly string _path;
        private readonly CultureInfo _cultureInfo;

        public CsvImportedTransactionModelReader(string path, CultureInfo cultureInfo)
        {
            _path = path;
            _cultureInfo = cultureInfo;
        }

        public async Task ReadImportedTransactionsAsync(int skip, int take, BlockingCollection<IList<ImportedTransactionEntity>> result)
        {
            using var reader = new StreamReader(_path);

            using var csv = new CsvReader(reader, new CsvConfiguration(_cultureInfo)
            {
                HasHeaderRecord = true,
            });

            var options = new TypeConverterOptions
            {
                NullValues =
                {
                    string.Empty,
                },
            };
            csv.Context.TypeConverterOptionsCache.AddOptions<string>(options);
            csv.Context.TypeConverterOptionsCache.AddOptions<int?>(options);

            result.Add(await csv.GetRecordsAsync<ImportedTransactionEntity>().ToListAsync().ConfigureAwait(false));
            result.CompleteAdding();
        }
    }

    private sealed class StdOutRelationalModelWriter : IRelationalModelWriter
    {
        private readonly CultureInfo _cultureInfo;

        public StdOutRelationalModelWriter(CultureInfo cultureInfo)
        {
            _cultureInfo = cultureInfo;
        }

        public async Task WriteRelationalModelAsync(IEnumerable<IList<MeteringPointEntity>> relationalModelBatches, IEnumerable<IList<QuarantinedMeteringPointEntity>> quarantined)
        {
            var sb = new StringBuilder();

            var meteringPointEntities = relationalModelBatches.SelectMany(x => x).ToList();

            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Cast<object>().ToList()));
            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Select(x => x.MeteringPointPeriods).SelectMany(x => x).Cast<object>().ToList()));
            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Select(x => x.MeteringPointPeriods).SelectMany(x => x.Select(y => y.InstallationAddress)).Cast<object>().ToList()));
            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x).Cast<object>().ToList()));
            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.EnergySupplyPeriods)).Cast<object>().ToList()));
            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.EnergySupplyPeriods).SelectMany(z => z.Contacts)).Cast<object>().ToList()));
            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.EnergySupplyPeriods).SelectMany(z => z.Contacts.Select(i => i.ContactAddress))).Where(j => j is not null).Cast<object>().ToList()));
            sb.Append(
                PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.ElectricalHeatingPeriods)).Cast<object>().ToList()));

            var quarantinedMeteringPoints = quarantined.SelectMany(x => x).ToList();
            sb.Append(
                PrettyPrintObjects(quarantinedMeteringPoints.Cast<object>().ToList()));

            await Console.Out.WriteLineAsync(sb.ToString()).ConfigureAwait(false);
        }

        private string PrettyPrintObjects(IList<object> items)
        {
            if (items.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine(items[0].GetType().Name.Replace("Entity", string.Empty, StringComparison.InvariantCulture));
            var properties = items.First().GetType()
                .GetProperties()
                // ignore collections
                .Where(p => !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string))
                // ignore complex properties of complex types
                .Where(p => p.PropertyType.IsPrimitive || p.PropertyType.IsValueType || p.PropertyType == typeof(string))
                .ToArray();

            var columnWidths = properties
                .Select(p => items
                    .Select(i => p.GetValue(i)?.ToString()?.Length ?? 0)
                    .Prepend(p.Name.Length)
                    .Max())
                .ToArray();

            var separator = "+-" + string.Join("-+-", columnWidths.Select(w => new string('-', w))) + "-+";
            sb.AppendLine(separator);

            var header = "| " + string.Join(" | ", properties
                .Select((p, i) => p.Name.PadRight(columnWidths[i]))) + " |";
            sb.AppendLine(header);
            sb.AppendLine(separator);

            foreach (var item in items)
            {
                var rowText = "| " + string.Join(" | ", properties
                    .Select((p, i) =>
                    {
                        var value = p.GetValue(item);
                        return value is DateTimeOffset dateTimeOffset
                            ? dateTimeOffset.ToString(_cultureInfo).PadRight(columnWidths[i])
                            : value?.ToString()?.PadRight(columnWidths[i]) ?? string.Empty.PadRight(columnWidths[i]);
                    })) + " |";
                sb.AppendLine(rowText);
            }

            sb.AppendLine(separator);
            var prettyPrintObjects = sb.ToString();
            return !string.IsNullOrWhiteSpace(prettyPrintObjects) ? prettyPrintObjects + "\n" : prettyPrintObjects;
        }
    }
}
