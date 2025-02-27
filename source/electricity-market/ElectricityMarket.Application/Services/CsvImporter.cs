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

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Energinet.DataHub.ElectricityMarket.Application.Models;

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public class CsvImporter : ICsvImporter
{
    private readonly CultureInfo _cultureInfo = new("da-DK");

    public async Task<IEnumerable<ImportedTransactionRecord>> ImportAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        var conf = new CsvConfiguration(_cultureInfo)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            DetectDelimiter = true
        };
        using var csv = new CsvReader(reader, conf);
        var records = csv.GetRecordsAsync<ImportedTransactionRecord>();

        return await records.ToListAsync().ConfigureAwait(false);
    }
}
