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

using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Energinet.DataHub.ElectricityMarket.Application.Converter;

public class TransactionTypeConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            var transactionTypeArray = JsonSerializer.Deserialize<string[]>(text) ?? Array.Empty<string>();
            if (transactionTypeArray.Length > 1)
            {
                throw new InvalidOperationException("Multiple transaction types are not supported");
            }

            return transactionTypeArray.FirstOrDefault();
        }

        return base.ConvertFromString(text, row, memberMapData);
    }
}
