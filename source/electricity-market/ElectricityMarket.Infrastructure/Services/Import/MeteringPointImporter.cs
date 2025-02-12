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
using System.Globalization;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class MeteringPointImporter : IMeteringPointImporter
{
    private readonly IEnumerable<ITransactionImporter> _transactionImporters;
    private readonly ILogger<MeteringPointImporter> _logger;

    public MeteringPointImporter(
        IEnumerable<ITransactionImporter> transactionImporters,
        ILogger<MeteringPointImporter> logger)
    {
        _transactionImporters = transactionImporters;
        _logger = logger;
    }

    public async Task ImportAsync(MeteringPointEntity meteringPoint, IEnumerable<ImportedTransactionEntity> importedTransactions)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(importedTransactions);

        foreach (var transactionEntity in importedTransactions)
        {
            if (string.IsNullOrWhiteSpace(meteringPoint.Identification))
            {
                meteringPoint.Identification = transactionEntity.metering_point_id.ToString(CultureInfo.InvariantCulture);
            }

            var handled = false;

            // TODO: What shall we do here?
            foreach (var transactionImporter in _transactionImporters)
            {
                var result = await transactionImporter.ImportAsync(meteringPoint, transactionEntity).ConfigureAwait(false);
                if (result.Status == TransactionImporterResultStatus.Handled)
                {
                    handled = true;
                    break;
                }
            }

            if (!handled)
            {
                _logger.LogWarning("Unhandled transaction trans_doss_id: {TransactionId} state_id: {StateId}", transactionEntity.btd_business_trans_doss_id, transactionEntity.metering_point_state_id);
            }
        }
    }
}
