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
    private readonly IReadOnlySet<string> _ignoredTransactions = new HashSet<string> { "CALCENC", "CALCTSSBM", "CNCCNSMRK", "CNCCNSOTH", "CNCREADMRK", "CNCREADOTH", "EOSMDUPD", "HEATYTDREQ", "HISANNCON", "HISANNREQ", "HISDATREQ", "INITCNCCOS", "LDSHRSBM", "MNTCHRGLNK", "MOVEINGO", "MSTDATREQ", "MSTDATRNO", "MTRDATREQ", "MTRDATSBM", "QRYCHARGE", "QRYMSDCHG", "SBMCNTADR", "SBMEACES", "SBMEACGO", "SBMMRDES", "SBMMRDGO", "SERVICEREQ", "STOPFEE", "STOPSUB", "STOPTAR", "UNEXPERR", "UPDBLCKLNK", "VIEWACCNO", "VIEWMOVES", "VIEWMP", "VIEWMPNO", "VIEWMTDNO" };

    private readonly IReadOnlySet<string> _changeTransactions = new HashSet<string> { "BLKMERGEGA", "BULKCOR", "CHGSETMTH", "CLSDWNMP", "CONNECTMP", "CREATEMP", "CREATESMP", "CREHISTMP", "CREMETER", "HTXCOR", "LNKCHLDMP", "MANCOR", "STPMETER", "ULNKCHLDMP", "UPDATESMP", "UPDHISTMP", "UPDMETER", "UPDPRDOBL", "XCONNECTMP", "MSTDATSBM" };

    private readonly IReadOnlySet<string> _unhandledTransactions = new HashSet<string> { "BLKBANKBS", "BULKCOR", "MANCOR" };

    public Task<(bool Imported, string Message)> ImportAsync(MeteringPointEntity meteringPoint, IEnumerable<ImportedTransactionEntity> importedTransactions)
    {
        ArgumentNullException.ThrowIfNull(meteringPoint);
        ArgumentNullException.ThrowIfNull(importedTransactions);

        try
        {
            foreach (var transactionEntity in importedTransactions)
            {
                if (string.IsNullOrWhiteSpace(meteringPoint.Identification))
                {
                    meteringPoint.Identification = transactionEntity.metering_point_id.ToString(CultureInfo.InvariantCulture);
                }

                var result = TryImportTransaction(transactionEntity, meteringPoint);
                if (!result.Imported)
                {
                    return Task.FromResult(result);
                }
            }
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return Task.FromResult((false, $"Crash during import of MP: {meteringPoint.Identification} {ex}"));
        }

        return Task.FromResult((true, string.Empty));
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
            if (currentlyActiveMeteringPointPeriod.ValidTo != DateTimeOffset.MaxValue && currentlyActiveMeteringPointPeriod.ValidTo != importedTransaction.valid_to_date)
            {
                errorMessage = "Currently active mpps valid_to is neither infinity nor equal to the valid_to of the imported transaction";
                return false;
            }

            if (currentlyActiveMeteringPointPeriod.ValidFrom == importedTransaction.valid_from_date)
            {
                if (currentlyActiveMeteringPointPeriod.ValidTo == DateTimeOffset.MaxValue)
                {
                    meteringPointPeriod.ValidTo = DateTimeOffset.MaxValue;
                }
                else
                {
                    meteringPointPeriod.ValidTo = meteringPoint.MeteringPointPeriods
                        .Where(p => p.ValidFrom > meteringPointPeriod.ValidFrom && p.RetiredBy == null)
                        .Min(p => p.ValidFrom);
                }

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

        meteringPoint.MeteringPointPeriods.Add(meteringPointPeriod);
        errorMessage = null;
        return true;
    }

    private static bool TryAddCommercialRelation(
        ImportedTransactionEntity importedTransaction,
        MeteringPointEntity meteringPoint,
        [NotNullWhen(false)] out string? errorMessage)
    {
        var transactionType = importedTransaction.transaction_type.TrimEnd();

        if (meteringPoint.CommercialRelations.Count > 0)
        {
            if (transactionType is "MOVEINES")
            {
                var prevCrs = meteringPoint.CommercialRelations
                    .Where(x => x.StartDate >= importedTransaction.valid_from_date)
                    .ToList();

                if (prevCrs.Count == 0)
                {
                    var overlappingCr = meteringPoint.CommercialRelations
                        .Where(x => x.StartDate < x.EndDate)
                        .OrderBy(x => x.StartDate)
                        .FirstOrDefault(x => x.StartDate < importedTransaction.valid_from_date && x.EndDate >= importedTransaction.valid_from_date);

                    if (overlappingCr != null)
                    {
                        overlappingCr.EndDate = importedTransaction.valid_from_date;
                    }

                    var activeEsp = meteringPoint.CommercialRelations
                        .Where(x => x.StartDate < x.EndDate)
                        .SelectMany(x => x.EnergySupplyPeriods)
                        .OrderBy(x => x.ValidFrom)
                        .LastOrDefault(x => x.ValidFrom <= importedTransaction.valid_from_date && x.RetiredBy == null);

                    if (activeEsp == null)
                    {
                        meteringPoint.CommercialRelations.Add(CommercialRelationFactory.CreateCommercialRelation(importedTransaction));
                        errorMessage = string.Empty;
                        return true;
                    }
                }
            }
        }
        else
        {
            if (transactionType is "ENDSUPPLY" or "INCMOVEOUT" or "INCMOVEIN" or "INCMOVEMAN")
            {
                errorMessage = string.Empty;
                return true;
            }

            meteringPoint.CommercialRelations.Add(CommercialRelationFactory.CreateCommercialRelation(importedTransaction));
            errorMessage = string.Empty;
            return true;
        }

        errorMessage = "WIP";
        return false;
    }

    private (bool Imported, string Message) TryImportTransaction(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint)
    {
        var transactionType = importedTransaction.transaction_type.TrimEnd();
        var dossierStatus = importedTransaction.transaction_type.TrimEnd();
        var type = MeteringPointEnumMapper.MapDh2ToEntity(MeteringPointEnumMapper.MeteringPointTypes, importedTransaction.type_of_mp);

        if (_changeTransactions.Contains(transactionType) && !TryAddMeteringPointPeriod(importedTransaction, meteringPoint, out var addMeteringPointPeriodError))
            return (false, addMeteringPointPeriodError);

        if (_ignoredTransactions.Contains(transactionType))
            return (true, string.Empty);

        if (type is not "Production" and not "Consumption")
            return (true, string.Empty);

        if (dossierStatus is "CAN" or "CNL")
            return (true, string.Empty);

        if (_unhandledTransactions.Contains(transactionType))
            return (false, $"Unhandled transaction type {transactionType}");

        if (!TryAddCommercialRelation(importedTransaction, meteringPoint, out var addCommercialRelationError))
            return (false, addCommercialRelationError);

        return (true, string.Empty);
    }
}
