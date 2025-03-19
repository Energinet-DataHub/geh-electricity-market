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
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

namespace InMemImporter;

internal sealed class StdOutRelationalModelWriter : IRelationalModelWriter
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
        await Console.Out.WriteLineAsync(await _modelPrinter.PrintAsync(relationalModelBatches, quarantined, _cultureInfo, html: true).ConfigureAwait(false)).ConfigureAwait(false);
    }
}
