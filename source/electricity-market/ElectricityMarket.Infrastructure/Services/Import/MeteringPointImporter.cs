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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Mappers;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class MeteringPointImporter : IMeteringPointImporter
{
    private static readonly IReadOnlySet<string> _ignoredTransactions = new HashSet<string> { "BLKCHGBRP", "CALCENC", "CALCTSSBM", "CNCCNSMRK", "CNCCNSOTH", "CNCREADMRK", "CNCREADOTH", "EOSMDUPD", "HEATYTDREQ", "HISANNCON", "HISANNREQ", "HISDATREQ", "INITCNCCOS", "LDSHRSBM", "LNKCHLDMP", "MNTCHRGLNK", "MOVEINGO", "MSTDATREQ", "MSTDATRNO", "MTRDATREQ", "MTRDATSBM", "QRYCHARGE", "QRYMSDCHG", "SBMCNTADR", "SBMEACES", "SBMEACGO", "SBMMRDES", "SBMMRDGO", "SERVICEREQ", "STOPFEE", "STOPSUB", "STOPTAR", "UNEXPERR", "ULNKCHLDMP", "UPDBLCKLNK", "VIEWACCNO", "VIEWMOVES", "VIEWMP", "VIEWMPNO", "VIEWMTDNO" };

    private static readonly IReadOnlySet<string> _changeTransactions = new HashSet<string> { "BLKMERGEGA", "BULKCOR", "CHGSETMTH", "CLSDWNMP", "CONNECTMP", "CREATEMP", "CREATESMP", "CREHISTMP", "CREMETER", "DATAMIG", "ENDSUPPLY", "MANCOR", "MSTDATSBM", "STPMETER", "UPDATESMP", "UPDHISTMP", "UPDMETER", "UPDPRDOBL", "XCONNECTMP", "HTXCOR" };

    private static readonly IReadOnlySet<string> _unhandledTransactions = new HashSet<string> { "BLKBANKBS", "BLKCHGBRP" };

    private static readonly DateTimeOffset _importCutoff = new(2018, 12, 31, 23, 0, 0, TimeSpan.Zero);

    private readonly ILogger<MeteringPointImporter> _logger;

    public MeteringPointImporter(ILogger<MeteringPointImporter> logger)
    {
        _logger = logger;
    }

    public Task<(bool Imported, string Message)> ImportAsync(MeteringPointEntity meteringPoint, IEnumerable<ImportedTransactionEntity> importedTransactions)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(importedTransactions);

        try
        {
            var hasAnyTransactions = false;

            foreach (var importedTransaction in importedTransactions)
            {
                meteringPoint.Identification = importedTransaction.metering_point_id;

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

            if (dossierStatus is "CAN" or "CNL")
            {
                return (true, string.Empty);
            }

            var type = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.MeteringPointTypes, importedTransaction.type_of_mp);

            if (type is "SurplusProductionGroup6" or "InternalUse")
            {
                return (true, string.Empty);
            }

            if ((_changeTransactions.Contains(transactionType) || meteringPoint.MeteringPointPeriods.Count == 0) && !TryAddMeteringPointPeriod(importedTransaction, meteringPoint, out var addMeteringPointPeriodError))
                return (false, addMeteringPointPeriodError);

            if (_ignoredTransactions.Contains(transactionType))
                return (true, string.Empty);

            if (type is not "Production" and not "Consumption")
                return (true, string.Empty);

            if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id) && transactionType != "ENDSUPPLY" && transactionType != "CLSDWNMP")
                return (true, string.Empty);

            if (_unhandledTransactions.Contains(transactionType))
                return (false, $"Unhandled transaction type {transactionType}");

            if (!TryAddCommercialRelation(_logger, importedTransaction, meteringPoint, out var addCommercialRelationError))
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
        else
        {
            var nextMeteringPointPeriod = meteringPoint.MeteringPointPeriods
                .Where(x => x.RetiredBy == null && x.ValidFrom > importedTransaction.valid_from_date)
                .MinBy(x => x.ValidFrom);

            meteringPointPeriod.ValidTo = nextMeteringPointPeriod?.ValidFrom ?? DateTimeOffset.MaxValue;
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
        ILogger<MeteringPointImporter> logger,
        ImportedTransactionEntity importedTransaction,
        MeteringPointEntity meteringPoint,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;

        var transactionType = importedTransaction.transaction_type.TrimEnd();
        var allCrsOrdered = AllSavedValidCrs(meteringPoint).ToList();

        if (allCrsOrdered.Count > 0)
        {
            if (_changeTransactions.Contains(transactionType) && transactionType != "DATAMIG" && transactionType != "ENDSUPPLY" && transactionType != "CLSDWNMP" && transactionType != "HTXCOR" && transactionType != "MANCOR")
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

                case "HTXCOR" or "MANCOR":
                    {
                        var activeEsp = meteringPoint.CommercialRelations.Where(x => x.StartDate != x.EndDate)
                            .SelectMany(x => x.EnergySupplyPeriods)
                            .Where(x => x.ValidFrom <= importedTransaction.valid_from_date && x.RetiredBy == null)
                            .MaxBy(x => x.ValidFrom);

                        if (activeEsp == null)
                        {
                            HandleMoveIn(importedTransaction, meteringPoint);
                            return true;
                        }

                        if (activeEsp.WebAccessCode != importedTransaction.web_access_code?.Trim())
                        {
                            HandleMoveIn(importedTransaction, meteringPoint);
                            return true;
                        }

                        if (activeEsp.EnergySupplier != importedTransaction.balance_supplier_id?.Trim())
                        {
                            var commercialRelationEntity = allCrsOrdered.First(x => x.StartDate <= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);
                            HandleEspStuff(importedTransaction, meteringPoint, commercialRelationEntity);
                            return true;
                        }

                        if (!TryDetermineClientIdForChangeOfSupplier(importedTransaction, out errorMessage, allCrsOrdered, out var clientId)) return false;

                        var cr = CommercialRelationFactory.CreateCommercialRelation(importedTransaction);
                        cr.ClientId = clientId.Value;

                        return true;
                    }

                case "CHANGESUP" or "CHGSUPSHRT" or "MANCHGSUP":
                    {
                        if (!TryDetermineClientIdForChangeOfSupplier(importedTransaction, out errorMessage, allCrsOrdered, out var clientId)) return false;

                        var commercialRelationEntity = CommercialRelationFactory.CreateCommercialRelation(importedTransaction);
                        commercialRelationEntity.ClientId = clientId.Value;

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

                case "ENDSUPPLY" or "CLSDWNMP":
                    {
                        var commercialRelationEntity = allCrsOrdered.First(x => x.StartDate <= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);
                        commercialRelationEntity.EndDate = importedTransaction.valid_from_date;

                        TryCloseActiveCr(importedTransaction, meteringPoint);
                        HandleEspStuff(importedTransaction, meteringPoint, commercialRelationEntity);
                        return true;
                    }

                case "INCCHGSUP" or "INCMOVEAUT" or "INCMOVEIN" or "INCMOVEMAN":
                    {
                        var cr = allCrsOrdered.FirstOrDefault(x => x.StartDate >= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);

                        if (cr is null)
                        {
                            logger.LogWarning(
                                "MISMATCH_INC_CR: No overlapping CR found {TransactionType}. mp: {MeteringPointId}, btd_trans_doss_id: {BtdTransDossId}, valid_from: {ValidFrom}",
                                transactionType,
                                meteringPoint.Identification,
                                importedTransaction.btd_trans_doss_id,
                                importedTransaction.valid_from_date);
                            return true;
                        }

                        if ((transactionType is "INCCHGSUP" && !cr.EnergySupplyPeriods.Any(x => x.TransactionType is "CHANGESUP" or "CHGSUPSHRT" or "MANCHGSUP" && x.ValidFrom == importedTransaction.valid_from_date)) ||
                            (transactionType is not "INCCHGSUP" && !cr.EnergySupplyPeriods.Any(x => x.TransactionType == "MOVEINES" && x.ValidFrom == importedTransaction.valid_from_date)))
                        {
                            logger.LogWarning(
                                "MISMATCH_INC_CR: No match found for {TransactionType}. mp: {MeteringPointId}, btd_trans_doss_id: {BtdTransDossId}, valid_from: {ValidFrom}",
                                transactionType,
                                meteringPoint.Identification,
                                importedTransaction.btd_trans_doss_id,
                                importedTransaction.valid_from_date);
                            return true;
                        }

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

                        if (cr.ElectricalHeatingPeriods.Count > 0)
                        {
                            ElectricalHeatingPeriodEntity correctionEhp;

                            if (prevCr.ElectricalHeatingPeriods.Count > 0)
                            {
                                correctionEhp = prevCr.ElectricalHeatingPeriods.MaxBy(x => x.ValidFrom)!;
                            }
                            else
                            {
                                correctionEhp = new ElectricalHeatingPeriodEntity
                                {
                                    CreatedAt = importedTransaction.dh2_created,
                                    ValidFrom = importedTransaction.valid_from_date,
                                    ValidTo = DateTimeOffset.MaxValue,
                                    MeteringPointStateId = importedTransaction.metering_point_state_id,
                                    BusinessTransactionDosId = importedTransaction.btd_trans_doss_id,
                                    Active = importedTransaction.tax_reduction ?? false,
                                    TransactionType = transactionType,
                                };
                                prevCr.ElectricalHeatingPeriods.Add(correctionEhp);
                            }

                            var toBeRetired = cr.ElectricalHeatingPeriods.Where(x => x.RetiredBy == null);
                            foreach (var ehpToBeRetired in toBeRetired)
                            {
                                ehpToBeRetired.RetiredBy = correctionEhp;
                                ehpToBeRetired.RetiredAt = DateTimeOffset.UtcNow;
                            }
                        }

                        return true;
                    }

                case "MDCNSEHON" or "MDCNSEHOFF":
                    {
                        if (importedTransaction.tax_settlement_date is null)
                        {
                            errorMessage = $"{transactionType} transaction without tax_settlement_date";
                            return false;
                        }

                        var cr = allCrsOrdered.First(x => x.StartDate <= importedTransaction.valid_from_date && importedTransaction.valid_from_date < x.EndDate);

                        HandleElectricalHeating(importedTransaction, meteringPoint, cr);

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

        if (transactionType is not ("ENDSUPPLY" or "INCMOVEOUT" or "INCMOVEIN" or "INCMOVEMAN" or "CLSDWNMP"))
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
        return meteringPoint.CommercialRelations.OrderBy(x => x.StartDate);
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

            RetireEspStuff(importedTransaction, meteringPoint, changeEsp);
            return;
        }

        if (activeEsp.ValidFrom == importedTransaction.valid_from_date)
        {
            if (activeEsp.ValidTo == DateTimeOffset.MaxValue)
            {
                RetireEspStuff(importedTransaction, meteringPoint, changeEsp);
                return;
            }

            changeEsp.ValidTo = AllSavedValidCrs(meteringPoint)
                .SelectMany(x => x.EnergySupplyPeriods)
                .Where(x => x.ValidFrom > importedTransaction.valid_from_date)
                .Min(x => x.ValidFrom);

            activeEsp.RetiredBy = changeEsp;
            activeEsp.RetiredAt = DateTimeOffset.UtcNow;

            RetireEspStuff(importedTransaction, meteringPoint, changeEsp);
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
            RetireEspStuff(importedTransaction, meteringPoint, changeEsp);
        }
        else
        {
            changeEsp.ValidTo = AllSavedValidCrs(meteringPoint)
                .SelectMany(x => x.EnergySupplyPeriods)
                .Where(x => x.ValidFrom > importedTransaction.valid_from_date)
                .MinBy(x => x.ValidFrom)?.ValidFrom ?? DateTimeOffset.MaxValue;

            RetireEspStuff(importedTransaction, meteringPoint, changeEsp);
        }

        if (importedTransaction.tax_reduction == true &&
            importedTransaction.transaction_type.TrimEnd() is "MOVEINES" or "DATAMIG" or "CHANGESUP" or "CHGSUPSHRT" or "MANCHGSUP" or "MOVEOUTES" or "ENDSUPPLY")
        {
            HandleElectricalHeating(importedTransaction, meteringPoint, commercialRelationEntity);
        }
    }

    private static void RetireEspStuff(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint, EnergySupplyPeriodEntity changeEsp)
    {
        var espToRetire = AllSavedValidCrs(meteringPoint)
            .SelectMany(x => x.EnergySupplyPeriods)
            .Where(esp => esp.RetiredBy == null &&
                          esp != changeEsp &&
                          esp.ValidFrom <= importedTransaction.valid_from_date &&
                          esp.ValidTo > importedTransaction.valid_from_date);

        foreach (var retiredEsp in espToRetire)
        {
            retiredEsp.RetiredBy = changeEsp;
            retiredEsp.RetiredAt = DateTimeOffset.UtcNow;
        }
    }

    private static void HandleElectricalHeating(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint, CommercialRelationEntity cr)
    {
        var allSavedValidCrs = AllSavedValidCrs(meteringPoint).ToList();

        var activeEhp = allSavedValidCrs.SelectMany(x => x.ElectricalHeatingPeriods).Where(x => x.ValidFrom <= importedTransaction.valid_from_date && x.RetiredBy is null).MaxBy(x => x.ValidFrom);

        var changeEhp = new ElectricalHeatingPeriodEntity
        {
            CreatedAt = importedTransaction.dh2_created,
            ValidFrom = importedTransaction.valid_from_date,
            MeteringPointStateId = importedTransaction.metering_point_state_id,
            BusinessTransactionDosId = importedTransaction.btd_trans_doss_id,
            Active = importedTransaction.tax_reduction ?? false,
            TransactionType = importedTransaction.transaction_type.TrimEnd(),
        };

        cr.ElectricalHeatingPeriods.Add(changeEhp);

        if (activeEhp is null)
        {
            var nextEhp = cr.ElectricalHeatingPeriods.Where(x => x.ValidFrom > importedTransaction.valid_from_date && x.RetiredBy is null).MinBy(x => x.ValidFrom);
            changeEhp.ValidTo = nextEhp?.ValidFrom ?? DateTimeOffset.MaxValue;
        }
        else
        {
            if (activeEhp.ValidFrom == importedTransaction.valid_from_date)
            {
                activeEhp.RetiredBy = changeEhp;
            }
            else
            {
                var retBy = new ElectricalHeatingPeriodEntity
                {
                    ValidFrom = activeEhp.ValidFrom,
                    ValidTo = importedTransaction.valid_from_date,
                    Active = activeEhp.Active,
                    BusinessTransactionDosId = activeEhp.BusinessTransactionDosId,
                    MeteringPointStateId = activeEhp.MeteringPointStateId,
                    CreatedAt = activeEhp.CreatedAt,
                    TransactionType = activeEhp.TransactionType,
                };
                cr.ElectricalHeatingPeriods.Add(retBy);

                activeEhp.RetiredBy = retBy;
            }

            changeEhp.ValidTo = activeEhp.ValidTo == DateTimeOffset.MaxValue
                ? DateTimeOffset.MaxValue
                : cr.ElectricalHeatingPeriods.Where(x => x.ValidFrom > importedTransaction.valid_from_date && x.RetiredBy is null).Min(x => x.ValidFrom);

            activeEhp.RetiredAt = DateTimeOffset.UtcNow;
        }
    }

    private static bool TryDetermineClientIdForChangeOfSupplier(ImportedTransactionEntity importedTransaction, [NotNullWhen(false)] out string? errorMessage, List<CommercialRelationEntity> allCrsOrdered, [NotNullWhen(true)] out Guid? clientId)
    {
        var prevCr = allCrsOrdered.Where(x => x.StartDate < importedTransaction.valid_from_date && x.StartDate != x.EndDate).MaxBy(x => x.StartDate) ??
                     allCrsOrdered.SingleOrDefault(x => x.StartDate == importedTransaction.valid_from_date && x.StartDate != x.EndDate);

        if (prevCr != null)
        {
            if (prevCr.ClientId == null)
            {
                errorMessage = "No client id found on prevCR";
                clientId = null;
                return false;
            }

            clientId = prevCr.ClientId.Value;
        }
        else
        {
            clientId = Guid.NewGuid();
        }

        errorMessage = null;
        return true;
    }
}
