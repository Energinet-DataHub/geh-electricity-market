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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class BulkImporter : IBulkImporter, IDisposable
{
    private readonly BlockingCollection<IList<ImportedTransactionEntity>> _importedTransactions = new(500_000);
    private readonly BlockingCollection<IList<MeteringPointEntity>> _relationalModelBatches = new(10);
    private readonly List<List<QuarantinedMeteringPointEntity>> _quarantined = new();

    private readonly ILogger<BulkImporter> _logger;
    private readonly IImportedTransactionModelReader _importedTransactionModelReader;
    private readonly IRelationalModelWriter _relationalModelWriter;
    private readonly IMeteringPointImporter _meteringPointImporter;

    public BulkImporter(
        ILogger<BulkImporter> logger,
        IImportedTransactionModelReader importedTransactionModelReader,
        IRelationalModelWriter relationalModelWriter,
        IMeteringPointImporter meteringPointImporter)
    {
        _logger = logger;
        _importedTransactionModelReader = importedTransactionModelReader;
        _relationalModelWriter = relationalModelWriter;
        _meteringPointImporter = meteringPointImporter;
    }

    public async Task RunAsync(int skip, int take)
    {
        var read = Task.Run(async () =>
        {
            try
            {
                await _importedTransactionModelReader
                    .ReadImportedTransactionsAsync(skip, take, _importedTransactions)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReleaseBlockingCollections();
                _logger.LogError(ex, "Error during gold model read.");
                throw;
            }
        });

        var package = Task.Run(async () =>
        {
            try
            {
                await ImportAndPackageTransactionsAsync(skip + 1).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReleaseBlockingCollections();
                _logger.LogError(ex, "Error during package.");
                throw;
            }
        });

        var write = Task.Run(async () =>
        {
            try
            {
                await _relationalModelWriter
                    .WriteRelationalModelAsync(_relationalModelBatches.GetConsumingEnumerable(), _quarantined)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ReleaseBlockingCollections();
                _logger.LogError(ex, "Error during package.");
                throw;
            }
        });

        await Task.WhenAll(read, package, write).ConfigureAwait(false);

        void ReleaseBlockingCollections()
        {
            try
            {
                _importedTransactions.CompleteAdding();
                _importedTransactions.Dispose();
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                // ignored
            }

            try
            {
                _relationalModelBatches.CompleteAdding();
                _relationalModelBatches.Dispose();
            }
#pragma warning disable CA1031
            catch (Exception)
#pragma warning restore CA1031
            {
                // ignored
            }
        }
    }

    public void Dispose()
    {
        _importedTransactions.Dispose();
        _relationalModelBatches.Dispose();
    }

    private static void AssignPrimaryKeys(MeteringPointEntity meteringPointEntity, long initialMeteringPointPrimaryKey)
    {
        meteringPointEntity.Id = initialMeteringPointPrimaryKey;

        // MeteringPointPeriod
        {
            var meteringPointPeriodPrimaryKey = meteringPointEntity.Id * 10000;

            foreach (var meteringPointPeriodEntity in meteringPointEntity.MeteringPointPeriods)
            {
                meteringPointPeriodEntity.Id = meteringPointPeriodPrimaryKey++;
                meteringPointPeriodEntity.MeteringPointId = meteringPointEntity.Id;
                meteringPointPeriodEntity.InstallationAddressId = meteringPointPeriodEntity.Id;
                meteringPointPeriodEntity.InstallationAddress.Id = meteringPointPeriodEntity.Id;
            }

            foreach (var meteringPointPeriodEntity in meteringPointEntity.MeteringPointPeriods)
            {
                meteringPointPeriodEntity.RetiredById = meteringPointPeriodEntity.RetiredBy?.Id;
            }

            if (meteringPointPeriodPrimaryKey >= (meteringPointEntity.Id * 10000) + 10000 || meteringPointPeriodPrimaryKey <= 0)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, MeteringPointPeriod.");
        }

        // CommercialRelation
        {
            var commercialRelationPrimaryKey = meteringPointEntity.Id * 1000;

            foreach (var commercialRelationEntity in meteringPointEntity.CommercialRelations)
            {
                commercialRelationEntity.Id = commercialRelationPrimaryKey++;
                commercialRelationEntity.MeteringPointId = meteringPointEntity.Id;
            }

            if (commercialRelationPrimaryKey >= (meteringPointEntity.Id * 1000) + 1000 || commercialRelationPrimaryKey <= 0)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, CommercialRelation.");
        }

        // ElectricalHeating
        foreach (var commercialRelationEntity in meteringPointEntity.CommercialRelations)
        {
            var electricalHeatingPrimaryKey = commercialRelationEntity.Id * 10000;

            foreach (var electricalHeatingPeriodEntity in commercialRelationEntity.ElectricalHeatingPeriods)
            {
                electricalHeatingPeriodEntity.Id = electricalHeatingPrimaryKey++;
                electricalHeatingPeriodEntity.CommercialRelationId = commercialRelationEntity.Id;
            }

            foreach (var electricalHeatingPeriodEntity in commercialRelationEntity.ElectricalHeatingPeriods)
            {
                electricalHeatingPeriodEntity.RetiredById = electricalHeatingPeriodEntity.RetiredBy?.Id;
            }

            if (electricalHeatingPrimaryKey >= (commercialRelationEntity.Id * 10000) + 10000 || electricalHeatingPrimaryKey <= 0)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, ElectricalHeating.");
        }

        // EnergySupplyPeriod
        foreach (var commercialRelationEntity in meteringPointEntity.CommercialRelations)
        {
            var energySupplyPeriodPrimaryKey = commercialRelationEntity.Id * 1000;

            foreach (var energySupplyPeriodEntity in commercialRelationEntity.EnergySupplyPeriods)
            {
                energySupplyPeriodEntity.Id = energySupplyPeriodPrimaryKey++;
                energySupplyPeriodEntity.CommercialRelationId = commercialRelationEntity.Id;
            }

            foreach (var energySupplyPeriodEntity in commercialRelationEntity.EnergySupplyPeriods)
            {
                energySupplyPeriodEntity.RetiredById = energySupplyPeriodEntity.RetiredBy?.Id;
            }

            if (energySupplyPeriodPrimaryKey >= (commercialRelationEntity.Id * 1000) + 1000 || energySupplyPeriodPrimaryKey <= 0)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, EnergySupplyPeriod.");
        }

        // Contact
        foreach (var energySupplyPeriodEntity in meteringPointEntity.CommercialRelations.SelectMany(cr => cr.EnergySupplyPeriods))
        {
            var contactPrimaryKey = energySupplyPeriodEntity.Id * 1000;

            foreach (var contactEntity in energySupplyPeriodEntity.Contacts)
            {
                contactEntity.Id = contactPrimaryKey++;
                contactEntity.EnergySupplyPeriodId = energySupplyPeriodEntity.Id;

                if (contactEntity.ContactAddress != null)
                {
                    contactEntity.ContactAddressId = contactEntity.Id;
                    contactEntity.ContactAddress.Id = contactEntity.Id;
                }
            }

            if (contactPrimaryKey >= (energySupplyPeriodEntity.Id * 1000) + 1000 || contactPrimaryKey <= 0)
                throw new InvalidOperationException($"Primary key overflow for {meteringPointEntity.Identification}, Contact.");
        }
    }

    private async Task ImportAndPackageTransactionsAsync(long initialMeteringPointPrimaryKey)
    {
        const int capacity = 30000;
        const int quarantineCapacity = 30000;

        var sw = Stopwatch.StartNew();
        var batch = new List<MeteringPointEntity>(capacity);
        var quarantineBatch = new List<QuarantinedMeteringPointEntity>(quarantineCapacity);

        foreach (var transactionsForOneMp in _importedTransactions.GetConsumingEnumerable())
        {
            if (batch.Count == capacity)
            {
                _relationalModelBatches.Add(batch);
                _logger.LogWarning("A relational batch was prepared in {BatchTime} ms.", sw.ElapsedMilliseconds);

                sw = Stopwatch.StartNew();
                batch = new List<MeteringPointEntity>(capacity);
            }

            if (quarantineBatch.Count == quarantineCapacity)
            {
                _quarantined.Add(quarantineBatch);
                quarantineBatch = new List<QuarantinedMeteringPointEntity>(capacity);
            }

            var meteringPoint = new MeteringPointEntity() { Version = 0 };

            var (imported, message) = await _meteringPointImporter
                .ImportAsync(meteringPoint, transactionsForOneMp)
                .ConfigureAwait(false);

            if (imported)
            {
                AssignPrimaryKeys(meteringPoint, initialMeteringPointPrimaryKey++);
                batch.Add(meteringPoint);
            }
            else
            {
                // add to quarantine
                quarantineBatch.Add(new QuarantinedMeteringPointEntity
                {
                    Identification = meteringPoint.Identification,
                    Message = message,
                });
            }
        }

        if (batch.Count > 0)
        {
            _relationalModelBatches.Add(batch);
        }

        if (quarantineBatch.Count > 0)
        {
            _quarantined.Add(quarantineBatch);
        }

        _relationalModelBatches.CompleteAdding();
    }
}
