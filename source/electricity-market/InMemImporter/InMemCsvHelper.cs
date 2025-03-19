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

namespace InMemImporter;

public static class InMemCsvHelper
{
    public static (CultureInfo CultureInfo, string Csv) PreapareCsv(string rawCsv)
    {
        ArgumentNullException.ThrowIfNull(rawCsv);

        var lines = rawCsv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var cultureInfo = lines[0].Contains(',', StringComparison.InvariantCultureIgnoreCase)
            ? CultureInfo.GetCultureInfo("en-US")
            : CultureInfo.GetCultureInfo("da-DK");

        var del = cultureInfo.Name == "da-DK" ? ';' : ',';

        var content = string.Join(Environment.NewLine, lines.Select((x, i) => $"{(i == 0 ? "Id" : "0")}{del}{x}"));

        return (cultureInfo, content);
    }
}
