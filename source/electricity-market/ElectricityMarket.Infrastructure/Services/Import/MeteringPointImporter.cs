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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class MeteringPointImporter : IMeteringPointImporter
{
    private static readonly IReadOnlySet<string> _ignoredTransactions = new HashSet<string> { "CALCENC", "CALCTSSBM", "CNCCNSMRK", "CNCCNSOTH", "CNCREADMRK", "CNCREADOTH", "EOSMDUPD", "HEATYTDREQ", "HISANNCON", "HISANNREQ", "HISDATREQ", "INITCNCCOS", "LDSHRSBM", "MNTCHRGLNK", "MOVEINGO", "MSTDATREQ", "MSTDATRNO", "MTRDATREQ", "MTRDATSBM", "QRYCHARGE", "QRYMSDCHG", "SBMCNTADR", "SBMEACES", "SBMEACGO", "SBMMRDES", "SBMMRDGO", "SERVICEREQ", "STOPFEE", "STOPSUB", "STOPTAR", "UNEXPERR", "UPDBLCKLNK", "VIEWACCNO", "VIEWMOVES", "VIEWMP", "VIEWMPNO", "VIEWMTDNO" };

    private static readonly IReadOnlySet<string> _changeTransactions = new HashSet<string> { "BLKMERGEGA", "BULKCOR", "CHGSETMTH", "CLSDWNMP", "CONNECTMP", "CREATEMP", "CREATESMP", "CREHISTMP", "CREMETER", "DATAMIG", "ENDSUPPLY", "LNKCHLDMP", "MANCOR", "MSTDATSBM", "STPMETER", "ULNKCHLDMP", "UPDATESMP", "UPDHISTMP", "UPDMETER", "UPDPRDOBL", "XCONNECTMP", "HTXCOR" };

    private static readonly IReadOnlySet<string> _unhandledTransactions = new HashSet<string> { "BLKBANKBS", "BLKCHGBRP" };

    private static readonly DateTimeOffset _importCutoff = new(2016, 12, 31, 23, 0, 0, TimeSpan.Zero);

    public Task<(bool Imported, string Message)> ImportAsync(MeteringPointEntity meteringPoint, IEnumerable<ImportedTransactionEntity> importedTransactions)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(importedTransactions);

        try
        {
            var hasAnyTransactions = false;

            foreach (var importedTransaction in importedTransactions)
            {
                if (string.IsNullOrWhiteSpace(meteringPoint.Identification))
                {
                    meteringPoint.Identification = importedTransaction.metering_point_id.ToString(CultureInfo.InvariantCulture);
                }

                if (importedTransaction.valid_to_date < _importCutoff)
                    continue;

                var result = TryImportTransaction(importedTransaction);
                if (result.Imported)
                {
                    hasAnyTransactions = true;
                }
                else
                {
                    return Task.FromResult(result);
                }
            }

            if (!hasAnyTransactions)
            {
                return Task.FromResult((false, "All transactions discarded for MP."));
            }
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return Task.FromResult((false, $"Crash during import of MP: {ex}"));
        }

        return Task.FromResult((true, string.Empty));

        (bool Imported, string Message) TryImportTransaction(ImportedTransactionEntity importedTransaction)
        {
            var transactionType = importedTransaction.transaction_type.TrimEnd();
            var dossierStatus = importedTransaction.dossier_status?.TrimEnd();
            var type = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.MeteringPointTypes, importedTransaction.type_of_mp);

            if ((_changeTransactions.Contains(transactionType) || meteringPoint.MeteringPointPeriods.Count == 0) && !TryAddMeteringPointPeriod(importedTransaction, meteringPoint, out var addMeteringPointPeriodError))
                return (false, addMeteringPointPeriodError);

            if (_ignoredTransactions.Contains(transactionType))
                return (true, string.Empty);

            if (type is not "Production" and not "Consumption")
                return (true, string.Empty);

            if (dossierStatus is "CAN" or "CNL")
            {
                if (transactionType is not ("MOVEINES" or "CHANGESUP" or "CHGSUPSHRT" or "MANCHGSUP"))
                    return (true, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id) && transactionType != "ENDSUPPLY")
                return (true, string.Empty);

            if (_unhandledTransactions.Contains(transactionType))
                return (false, $"Unhandled transaction type {transactionType}");

            if (!TryAddCommercialRelation(importedTransaction, meteringPoint, out var addCommercialRelationError))
                return (false, addCommercialRelationError);

            return (true, string.Empty);
        }
    }

    private static bool TryAddMeteringPointPeriod(
        ImportedTransactionEntity importedTransaction,
        MeteringPointEntity meteringPoint,
        [NotNullWhen(false)] out string? errorMessage)
    {
        var meteringPointPeriod = MeteringPointPeriodFactory.CreateMeteringPointPeriod(importedTransaction);

        var currentlyActiveMeteringPointPeriod = meteringPoint.MeteringPointPeriods
            .Where(p => p.RetiredBy == null && p.ValidFrom <= importedTransaction.valid_from_date)
            .MaxBy(x => x.ValidFrom);

        if (currentlyActiveMeteringPointPeriod != null)
        {
            // TODO:
            if (currentlyActiveMeteringPointPeriod.ValidTo != DateTimeOffset.MaxValue && currentlyActiveMeteringPointPeriod.ValidTo != importedTransaction.valid_to_date)
            {
                errorMessage = "Currently active mpps valid_to is neither infinity nor equal to the valid_to of the imported transaction";
                return false;
            }

            if (currentlyActiveMeteringPointPeriod.ValidFrom == importedTransaction.valid_from_date)
            {
                meteringPointPeriod.ValidTo = currentlyActiveMeteringPointPeriod.ValidTo == DateTimeOffset.MaxValue
                    ? DateTimeOffset.MaxValue
                    : meteringPoint.MeteringPointPeriods
                        .Where(p => p.ValidFrom > meteringPointPeriod.ValidFrom && p.RetiredBy == null)
                        .Min(p => p.ValidFrom);

                currentlyActiveMeteringPointPeriod.RetiredAt = DateTimeOffset.UtcNow;
                currentlyActiveMeteringPointPeriod.RetiredBy = meteringPointPeriod;
            }
            else
            {
                var copy = MeteringPointPeriodFactory.CopyMeteringPointPeriod(currentlyActiveMeteringPointPeriod);

                copy.ValidTo = importedTransaction.valid_from_date;

                meteringPoint.MeteringPointPeriods.Add(copy);

                currentlyActiveMeteringPointPeriod.RetiredAt = DateTimeOffset.UtcNow;
                currentlyActiveMeteringPointPeriod.RetiredBy = copy;

                meteringPointPeriod.ValidTo = currentlyActiveMeteringPointPeriod.ValidTo == DateTimeOffset.MaxValue
                    ? DateTimeOffset.MaxValue
                    : meteringPoint.MeteringPointPeriods
                        .Where(p => p.ValidFrom > meteringPointPeriod.ValidFrom && p.RetiredBy == null)
                        .Min(p => p.ValidFrom);
            }
        }

        if (importedTransaction.transaction_type.Trim() == "CLSDWNMP")
        {
            var futureClosedPeriods = meteringPoint.MeteringPointPeriods
                .Where(p => p.RetiredBy == null && p.ValidFrom > importedTransaction.valid_from_date);

            foreach (var futureClosedPeriod in futureClosedPeriods)
            {
                futureClosedPeriod.RetiredBy = meteringPointPeriod;
                futureClosedPeriod.RetiredAt = DateTimeOffset.UtcNow;
            }
        }

        meteringPoint.MeteringPointPeriods.Add(meteringPointPeriod);
        errorMessage = null;
        return true;
    }

    private static bool TryAddCommercialRelation(
        ImportedTransactionEntity importedTransaction,
        MeteringPointEntity meteringPoint,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;

        var transactionType = importedTransaction.transaction_type.TrimEnd();
        var allCrsOrdered = AllSavedValidCrs(meteringPoint).ToList();

        if (allCrsOrdered.Count > 0)
        {
            if (_changeTransactions.Contains(transactionType) && transactionType != "DATAMIG")
            {
                return true;
            }

            switch (transactionType)
            {
                case "MOVEINES" or "DATAMIG":
                    {
                        HandleMoveIn(importedTransaction, meteringPoint);
                        return true;
                    }

                case "CHANGESUP" or "CHGSUPSHRT" or "MANCHGSUP":
                    {
                        var commercialRelationEntity = CommercialRelationFactory.CreateCommercialRelation(importedTransaction);
                        commercialRelationEntity.ClientId = allCrsOrdered.Last(x => x.StartDate < importedTransaction.valid_from_date).ClientId;

                        TryCloseActiveCr(importedTransaction, meteringPoint);
                        HandleEspStuff(importedTransaction, meteringPoint, commercialRelationEntity);

                        meteringPoint.CommercialRelations.Add(commercialRelationEntity);
                        return true;
                    }

                case "MOVEOUTES":
                    {
                        HandleMoveOut(importedTransaction, meteringPoint);
                        return true;
                    }

                case "ENDSUPPLY":
                    {
                        var commercialRelationEntity = allCrsOrdered.First(x => x.StartDate <= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);
                        commercialRelationEntity.EndDate = importedTransaction.valid_from_date;

                        TryCloseActiveCr(importedTransaction, meteringPoint);
                        HandleEspStuff(importedTransaction, meteringPoint, commercialRelationEntity);
                        return true;
                    }

                case "INCCHGSUP" or "INCMOVEAUT" or "INCMOVEIN" or "INCMOVEMAN":
                    {
                        var cr = allCrsOrdered.First(x => x.StartDate >= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);
                        var prevCr = allCrsOrdered.Last(x => x.StartDate < cr.StartDate);
                        var nextCr = allCrsOrdered.FirstOrDefault(x => x.StartDate > cr.StartDate);

                        cr.EndDate = cr.StartDate;
                        prevCr.EndDate = nextCr?.StartDate ?? DateTimeOffset.MaxValue;
                        var correctionEsp = CommercialRelationFactory.CreateEnergySupplyPeriodEntity(importedTransaction);
                        correctionEsp.ValidTo = DateTimeOffset.MaxValue;

                        foreach (var toBeRetired in cr.EnergySupplyPeriods.Where(x => x.RetiredBy == null))
                        {
                            toBeRetired.RetiredBy = correctionEsp;
                            toBeRetired.RetiredAt = DateTimeOffset.UtcNow;
                        }

                        prevCr.EnergySupplyPeriods.Add(correctionEsp);
                        return true;
                    }

                case "MDCNSEHON":
                    {
                        if (importedTransaction.tax_settlement_date is null)
                        {
                            errorMessage = "MDCNSEHON transaction without tax_settlement_date";
                            return false;
                        }

                        var cr = allCrsOrdered.First(x => x.StartDate <= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);

                        cr.ElectricalHeatingPeriods.Add(new ElectricalHeatingPeriodEntity
                        {
                            CreatedAt = importedTransaction.dh2_created,
                            ValidFrom = importedTransaction.tax_settlement_date.Value,
                            ValidTo = DateTimeOffset.MaxValue,
                            MeteringPointStateId = importedTransaction.metering_point_state_id,
                            BusinessTransactionDosId = importedTransaction.btd_trans_doss_id,
                            Active = true,
                            TransactionType = transactionType,
                        });

                        return true;
                    }

                default:
                    {
                        var commercialRelationEntity = allCrsOrdered.First(x => x.StartDate <= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);
                        HandleEspStuff(importedTransaction, meteringPoint, commercialRelationEntity);
                        return true;
                    }
            }
        }

        if (transactionType is not ("ENDSUPPLY" or "INCMOVEOUT" or "INCMOVEIN" or "INCMOVEMAN"))
        {
            if (transactionType is "MOVEOUTES")
            {
                HandleMoveOut(importedTransaction, meteringPoint);
            }
            else
            {
                HandleMoveIn(importedTransaction, meteringPoint);
            }
        }

        return true;
    }

    private static IEnumerable<CommercialRelationEntity> AllSavedValidCrs(MeteringPointEntity meteringPoint)
    {
        return meteringPoint.CommercialRelations
            .Where(x => x.StartDate < x.EndDate)
            .OrderBy(x => x.StartDate);
    }

    private static void HandleMoveOut(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint)
    {
        var commercialRelationEntity = CommercialRelationFactory.CreateCommercialRelation(importedTransaction);
        commercialRelationEntity.ClientId = null;

        TryCloseActiveCr(importedTransaction, meteringPoint);
        HandleEspStuff(importedTransaction, meteringPoint, commercialRelationEntity);

        meteringPoint.CommercialRelations.Add(commercialRelationEntity);
    }

    private static void HandleMoveIn(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint)
    {
        var commercialRelation = CommercialRelationFactory.CreateCommercialRelation(importedTransaction);

        var activeCr = AllSavedValidCrs(meteringPoint)
            .FirstOrDefault(x => x.StartDate <= importedTransaction.valid_from_date && x.EndDate > importedTransaction.valid_from_date);

        if (activeCr != null)
        {
            var futures = AllSavedValidCrs(meteringPoint)
                .Where(x => x.StartDate > importedTransaction.valid_from_date && activeCr.ClientId == x.ClientId)
                .ToList();

            foreach (var entity in futures)
            {
                entity.EndDate = entity.StartDate;
            }

            if (futures.Count != 0)
            {
                var commercialRelationEntities = AllSavedValidCrs(meteringPoint).ToList();
                var nextFuture = commercialRelationEntities.IndexOf(futures.Last()) + 1;
                if (nextFuture < commercialRelationEntities.Count - 1)
                {
                    commercialRelation.EndDate = commercialRelationEntities[nextFuture].StartDate;
                }
            }
        }
        else
        {
            var nextFutureCr = AllSavedValidCrs(meteringPoint)
                .Where(x => x.StartDate > importedTransaction.valid_from_date)
                .MinBy(x => x.StartDate);

            if (nextFutureCr != null)
            {
                commercialRelation.EndDate = nextFutureCr.StartDate;
            }
        }

        TryCloseActiveCr(importedTransaction, meteringPoint);

        HandleEspStuff(importedTransaction, meteringPoint, commercialRelation);

        meteringPoint.CommercialRelations.Add(commercialRelation);
    }

    private static void TryCloseActiveCr(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint)
    {
        var activeCr = AllSavedValidCrs(meteringPoint)
            .FirstOrDefault(x => x.StartDate <= importedTransaction.valid_from_date && x.EndDate > importedTransaction.valid_from_date);

        if (activeCr != null)
        {
            activeCr.EndDate = importedTransaction.valid_from_date;
        }
    }

    private static void HandleEspStuff(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint, CommercialRelationEntity commercialRelationEntity)
    {
        var (activeCr, activeEsp) = AllSavedValidCrs(meteringPoint)
            .SelectMany(cr => cr.EnergySupplyPeriods.Select(esp => (cr, esp)))
            .OrderBy(group => group.esp.ValidFrom)
            .LastOrDefault(group => group.esp.ValidFrom <= importedTransaction.valid_from_date && group.esp.RetiredBy == null);

        var changeEsp = CommercialRelationFactory.CreateEnergySupplyPeriodEntity(importedTransaction);
        commercialRelationEntity.EnergySupplyPeriods.Add(changeEsp);

        if (activeEsp == null)
        {
            changeEsp.ValidTo = AllSavedValidCrs(meteringPoint)
                .SelectMany(x => x.EnergySupplyPeriods)
                .Where(x => x.ValidFrom > importedTransaction.valid_from_date)
                .MinBy(x => x.ValidFrom)?.ValidFrom ?? DateTimeOffset.MaxValue;
            return;
        }

        if (activeEsp.ValidFrom == importedTransaction.valid_from_date)
        {
            if (activeEsp.ValidTo == DateTimeOffset.MaxValue)
            {
                return;
            }

            changeEsp.ValidTo = AllSavedValidCrs(meteringPoint)
                .SelectMany(x => x.EnergySupplyPeriods)
                .Where(x => x.ValidFrom > importedTransaction.valid_from_date)
                .Min(x => x.ValidFrom);

            activeEsp.RetiredBy = changeEsp;
            activeEsp.RetiredAt = DateTimeOffset.UtcNow;

            return;
        }

        var retiredBy = CommercialRelationFactory.CopyEnergySupplyPeriod(activeEsp);
        retiredBy.ValidTo = importedTransaction.valid_from_date;

        activeEsp.RetiredBy = retiredBy;
        activeEsp.RetiredAt = DateTimeOffset.UtcNow;
        activeCr.EnergySupplyPeriods.Add(retiredBy);

        if (activeEsp.ValidTo == DateTimeOffset.MaxValue)
        {
            changeEsp.ValidTo = DateTimeOffset.MaxValue;
            return;
        }

        changeEsp.ValidTo = AllSavedValidCrs(meteringPoint)
            .SelectMany(x => x.EnergySupplyPeriods)
            .Where(x => x.ValidFrom > importedTransaction.valid_from_date)
            .MinBy(x => x.ValidFrom)?.ValidFrom ?? DateTimeOffset.MaxValue;
    }
}
