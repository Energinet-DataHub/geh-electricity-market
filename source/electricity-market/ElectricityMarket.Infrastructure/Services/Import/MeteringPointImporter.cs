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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class MeteringPointImporter : IMeteringPointImporter
{
    private readonly ILogger<MeteringPointImporter> _logger;

    private readonly IReadOnlySet<string> _ignoredTransactions = new HashSet<string> { "CALCENC", "CALCTSSBM", "CNCCNSMRK", "CNCCNSOTH", "CNCREADMRK", "CNCREADOTH", "EOSMDUPD", "HEATYTDREQ", "HISANNCON", "HISANNREQ", "HISDATREQ", "INITCNCCOS", "LDSHRSBM", "MNTCHRGLNK", "MOVEINGO", "MSTDATREQ", "MSTDATRNO", "MTRDATREQ", "MTRDATSBM", "QRYCHARGE", "QRYMSDCHG", "SBMCNTADR", "SBMEACES", "SBMEACGO", "SBMMRDES", "SBMMRDGO", "SERVICEREQ", "STOPFEE", "STOPSUB", "STOPTAR", "UNEXPERR", "UPDBLCKLNK", "VIEWACCNO", "VIEWMOVES", "VIEWMP", "VIEWMPNO", "VIEWMTDNO" };

    private readonly IReadOnlySet<string> _changeTransactions = new HashSet<string> { "BLKMERGEGA", "BULKCOR", "CHGSETMTH", "CLSDWNMP", "CONNECTMP", "CREATEMP", "CREATESMP", "CREHISTMP", "CREMETER", "HTXCOR", "LNKCHLDMP", "MANCOR", "STPMETER", "ULNKCHLDMP", "UPDATESMP", "UPDHISTMP", "UPDMETER", "UPDPRDOBL", "XCONNECTMP" };

    private readonly IReadOnlySet<string> _unhandledTransactions = new HashSet<string> { "BLKBANKBS", "BULKCOR", "HTXCOR", "INCCHGSUP", "INCMOVEAUT", "INCMOVEIN", "INCMOVEMAN", "MANCOR" };

    public MeteringPointImporter(ILogger<MeteringPointImporter> logger)
    {
        _logger = logger;
    }

    public Task ImportAsync(MeteringPointEntity meteringPoint, IEnumerable<ImportedTransactionEntity> importedTransactions)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(importedTransactions);

        foreach (var transactionEntity in importedTransactions)
        {
            if (string.IsNullOrWhiteSpace(meteringPoint.Identification))
            {
                meteringPoint.Identification = transactionEntity.metering_point_id.ToString(CultureInfo.InvariantCulture);
            }

            if (!TryImportTransaction(transactionEntity, meteringPoint))
            {
                _logger.LogWarning("Unhandled transaction trans_doss_id: {TransactionId} state_id: {StateId}", transactionEntity.btd_business_trans_doss_id, transactionEntity.metering_point_state_id);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private bool TryImportTransaction(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint)
    {
        var currentTransactionType = importedTransaction.transaction_type.TrimEnd();

        if (_ignoredTransactions.Contains(currentTransactionType))
            return true;

        // HTX is ignored for now.
        if (meteringPoint.MeteringPointPeriods.Count > 0 &&
            meteringPoint.MeteringPointPeriods.Max(p => p.ValidFrom) >= importedTransaction.valid_from_date)
        {
            return true;
        }

        var newMpPeriod = new MeteringPointPeriodEntity
        {
            ValidFrom = importedTransaction.valid_from_date,
            ValidTo = DateTimeOffset.MaxValue,
            BusinessTransactionDosId = importedTransaction.btd_business_trans_doss_id,
            MeteringPointStateId = importedTransaction.metering_point_state_id,
            TransactionType = importedTransaction.transaction_type,

            // ParentIdentification =
            CreatedAt = importedTransaction.dh2_created,
            GridAreaCode = importedTransaction.metering_grid_area_id,
            // OwnedBy =
            ConnectionState = ExternalMeteringPointConnectionStateMapper.Map(importedTransaction.physical_status_of_mp.TrimEnd()),
            Type = ExternalMeteringPointTypeMapper.Map(importedTransaction.type_of_mp.TrimEnd()),
            SubType = ExternalMeteringPointSubTypeMapper.Map(importedTransaction.sub_type_of_mp.TrimEnd()),
            // Resolution =
            Unit = ExternalMeteringPointUnitMapper.Map(importedTransaction.energy_timeseries_measure_unit.TrimEnd()),
            // ProductId =
            // SettlementGroup =
            // ScheduledMeterReadingMonth =
        };

        if (_changeTransactions.Contains(currentTransactionType))
        {
            var overlappingPeriod = meteringPoint.MeteringPointPeriods
                .SingleOrDefault(p => p.ValidTo > importedTransaction.valid_from_date);

            if (overlappingPeriod != null)
            {
                var closedOverlappingPeriod = new MeteringPointPeriodEntity
                {
                    ValidTo = importedTransaction.valid_from_date,

                    TransactionType = overlappingPeriod.TransactionType,
                    ParentIdentification = overlappingPeriod.ParentIdentification,
                    ValidFrom = overlappingPeriod.ValidFrom,
                    CreatedAt = overlappingPeriod.CreatedAt,
                    GridAreaCode = overlappingPeriod.GridAreaCode,
                    OwnedBy = overlappingPeriod.OwnedBy,
                    ConnectionState = overlappingPeriod.ConnectionState,
                    Type = overlappingPeriod.Type,
                    SubType = overlappingPeriod.SubType,
                    Resolution = overlappingPeriod.Resolution,
                    Unit = overlappingPeriod.Unit,
                    ProductId = overlappingPeriod.ProductId,
                    SettlementGroup = overlappingPeriod.SettlementGroup,
                    ScheduledMeterReadingMonth = overlappingPeriod.ScheduledMeterReadingMonth,
                    MeteringPointStateId = overlappingPeriod.MeteringPointStateId,
                    BusinessTransactionDosId = overlappingPeriod.BusinessTransactionDosId,
                };

                meteringPoint.MeteringPointPeriods.Add(closedOverlappingPeriod);

                overlappingPeriod.RetiredAt = importedTransaction.dh2_created; // TODO: Different from example.
                overlappingPeriod.RetiredBy = closedOverlappingPeriod;
            }
        }

        meteringPoint.MeteringPointPeriods.Add(newMpPeriod);

        if (newMpPeriod.Type is not "Production" and not "Consumption")
            return true;

        var applyNewCommercialRelation = false;
        var activeCommercialRelation = new CommercialRelationEntity
        {
            StartDate = importedTransaction.valid_from_date,
            EndDate = DateTimeOffset.MaxValue,
            ModifiedAt = importedTransaction.dh2_created,
        };

        if (currentTransactionType is "MOVEINES")
        {
            if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id))
                throw new InvalidOperationException($"Missing balance_supplier_id for imported transaction id: {importedTransaction.Id}.");

            activeCommercialRelation.EnergySupplier = importedTransaction.balance_supplier_id;
            activeCommercialRelation.CustomerId = Guid.NewGuid();
            applyNewCommercialRelation = true;
        }

        if (currentTransactionType is "CHANGESUP" or "CHGSUPSHRT" or "MANCHGSUP")
        {
            if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id))
                throw new InvalidOperationException($"Missing balance_supplier_id for imported transaction id: {importedTransaction.Id}.");

            var previousCommercialRelation = meteringPoint.CommercialRelations
                .Single(cr => cr.EndDate > importedTransaction.valid_from_date);

            activeCommercialRelation.EnergySupplier = importedTransaction.balance_supplier_id;
            activeCommercialRelation.CustomerId = previousCommercialRelation.CustomerId;
            applyNewCommercialRelation = true;
        }

        if (currentTransactionType is "MOVEOUTES")
        {
            if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id))
                throw new InvalidOperationException($"Missing balance_supplier_id for imported transaction id: {importedTransaction.Id}.");

            activeCommercialRelation.EnergySupplier = importedTransaction.balance_supplier_id;
            activeCommercialRelation.CustomerId = null;
            applyNewCommercialRelation = true;
        }

        if (currentTransactionType is "ENDSUPPLY")
        {
            activeCommercialRelation.EnergySupplier = string.Empty;
            activeCommercialRelation.CustomerId = null;
            applyNewCommercialRelation = true;
        }

        if (applyNewCommercialRelation)
        {
            var previousCommercialRelation = meteringPoint.CommercialRelations
                .Single(cr => cr.EndDate > importedTransaction.valid_from_date);

            previousCommercialRelation.EndDate = importedTransaction.valid_from_date;
            meteringPoint.CommercialRelations.Add(activeCommercialRelation);
        }
        else
        {
            if (_unhandledTransactions.Contains(currentTransactionType))
                return false;

            // TODO: Not matching Miro. Verify!
            activeCommercialRelation = meteringPoint.CommercialRelations
                .Single(cr => cr.EndDate > importedTransaction.valid_from_date);
        }

        var newEnergySupplyPeriod = new EnergySupplyPeriodEntity
        {
            ValidFrom = importedTransaction.valid_from_date,
            ValidTo = DateTimeOffset.MaxValue,
            CreatedAt = importedTransaction.dh2_created,
            BusinessTransactionDosId = importedTransaction.btd_business_trans_doss_id,
            WebAccessCode = importedTransaction.web_access_code ?? string.Empty, // TODO: This is probably wrong.
            EnergySupplier = importedTransaction.balance_supplier_id ?? "TODO: What?", // TODO: Fallback is sus.
        };

        // TODO: Not matching Miro. Verify!
        var overlappingEnergySupplyPeriod = activeCommercialRelation.EnergySupplyPeriods
            .SingleOrDefault(esp => esp.ValidTo > importedTransaction.valid_from_date);

        if (overlappingEnergySupplyPeriod == null)
        {
            activeCommercialRelation.EnergySupplyPeriods.Add(newEnergySupplyPeriod);
            return true;
        }

        var closedOverlappingEnergySupplyPeriod = new EnergySupplyPeriodEntity
        {
            ValidTo = importedTransaction.valid_from_date,

            ValidFrom = overlappingEnergySupplyPeriod.ValidFrom,
            CreatedAt = overlappingEnergySupplyPeriod.CreatedAt,
            BusinessTransactionDosId = overlappingEnergySupplyPeriod.BusinessTransactionDosId,
            WebAccessCode = overlappingEnergySupplyPeriod.WebAccessCode,
            EnergySupplier = overlappingEnergySupplyPeriod.WebAccessCode,
        };

        activeCommercialRelation.EnergySupplyPeriods.Add(closedOverlappingEnergySupplyPeriod);

        overlappingEnergySupplyPeriod.RetiredBy = closedOverlappingEnergySupplyPeriod;
        overlappingEnergySupplyPeriod.RetiredAt = importedTransaction.dh2_created; // TODO: Different from example.

        return true;
    }
}
