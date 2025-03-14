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

using Energinet.DataHub.ElectricityMarket.Application.Commands.Import;
using Energinet.DataHub.ElectricityMarket.Application.Interfaces;
using Energinet.DataHub.ElectricityMarket.Application.Services;
using MediatR;

namespace Energinet.DataHub.ElectricityMarket.Application.Handlers;

public sealed class ImportTransactionsHandler : IRequestHandler<ImportTransactionsCommand, long>
{
    private readonly ICsvImporter _csvImporter;
    private readonly IImportedTransactionRepository _importedTransactionRepository;

    public ImportTransactionsHandler(IImportedTransactionRepository importedTransactionRepository, ICsvImporter csvImporter)
    {
        _importedTransactionRepository = importedTransactionRepository;
        _csvImporter = csvImporter;
    }

    public async Task<long> Handle(ImportTransactionsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var transactions = await _csvImporter.ImportAsync(request.TransactionsStream).ConfigureAwait(false);

        var existingMeteringPoints = await _importedTransactionRepository.GetImportedTransactionsAsync(transactions).ConfigureAwait(false);

        var newTransactions = transactions.Where(t => existingMeteringPoints.All(x => t.metering_point_id != x));

        await _importedTransactionRepository.AddAsync(newTransactions).ConfigureAwait(false);

        return newTransactions.Count();
    }
}
