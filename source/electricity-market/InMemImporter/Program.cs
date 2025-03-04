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
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;
using Microsoft.Extensions.Logging.Abstractions;

namespace InMemImporter;

public static class Program
{
    public static async Task Main(params string[] args)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(args);

            var cultureInfo = CultureInfo.GetCultureInfo(args[0]);

            using var importer = new Importer(
                NullLogger<Importer>.Instance,
                new CsvImportedTransactionModelReader(args[1], cultureInfo),
                new StdOutRelationalModelWriter(new RelationalModelPrinter(), cultureInfo),
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
        private readonly IRelationalModelPrinter _modelPrinter;
        private readonly CultureInfo _cultureInfo;

        public StdOutRelationalModelWriter(IRelationalModelPrinter modelPrinter, CultureInfo cultureInfo)
        {
            _modelPrinter = modelPrinter;
            _cultureInfo = cultureInfo;
        }

        public async Task WriteRelationalModelAsync(IEnumerable<IList<MeteringPointEntity>> relationalModelBatches, IEnumerable<IList<QuarantinedMeteringPointEntity>> quarantined)
        {
            await Console.Out.WriteLineAsync(await _modelPrinter.PrintAsync(relationalModelBatches, quarantined, _cultureInfo).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}
