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

namespace InMemImporter;

internal sealed class CsvImportedTransactionModelReader : IImportedTransactionModelReader
{
    private readonly string _csv;
    private readonly CultureInfo _cultureInfo;

    public CsvImportedTransactionModelReader(string csv, CultureInfo cultureInfo)
    {
        _csv = csv;
        _cultureInfo = cultureInfo;
    }

    public async Task ReadImportedTransactionsAsync(int skip, int take, BlockingCollection<IList<ImportedTransactionEntity>> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(_csv));
        using var reader = new StreamReader(ms);

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
        csv.Context.TypeConverterCache.AddConverter<DateTimeOffset>(new CustomDateTimeConverter());
        csv.Context.TypeConverterOptionsCache.AddOptions<string>(options);
        csv.Context.TypeConverterOptionsCache.AddOptions<int?>(options);

        result.Add(await csv.GetRecordsAsync<ImportedTransactionEntity>().ToListAsync().ConfigureAwait(false));
        result.CompleteAdding();
    }

    private sealed class CustomDateTimeConverter : ITypeConverter
    {
        public object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return DateTimeOffset.MaxValue;
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            {
                return date;
            }

#pragma warning disable CA2201
            throw new Exception($"Invalid date format: {text}");
#pragma warning restore CA2201
        }

        public string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            throw new NotImplementedException();
        }
    }
}
