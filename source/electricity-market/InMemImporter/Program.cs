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

            var (cultureInfo, csv) = InMemCsvHelper.PreapareCsv(await File.ReadAllTextAsync(args[0]).ConfigureAwait(false));

            using var importer = new BulkImporter(
                NullLogger<BulkImporter>.Instance,
                new CsvImportedTransactionModelReader(csv, cultureInfo),
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
}
