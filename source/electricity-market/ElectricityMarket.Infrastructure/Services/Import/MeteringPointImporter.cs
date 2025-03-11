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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class MeteringPointImporter : IMeteringPointImporter
{
    private readonly IReadOnlySet<string> _ignoredTransactions = new HashSet<string> { "CALCENC", "CALCTSSBM", "CNCCNSMRK", "CNCCNSOTH", "CNCREADMRK", "CNCREADOTH", "EOSMDUPD", "HEATYTDREQ", "HISANNCON", "HISANNREQ", "HISDATREQ", "INITCNCCOS", "LDSHRSBM", "MNTCHRGLNK", "MOVEINGO", "MSTDATREQ", "MSTDATRNO", "MTRDATREQ", "MTRDATSBM", "QRYCHARGE", "QRYMSDCHG", "SBMCNTADR", "SBMEACES", "SBMEACGO", "SBMMRDES", "SBMMRDGO", "SERVICEREQ", "STOPFEE", "STOPSUB", "STOPTAR", "UNEXPERR", "UPDBLCKLNK", "VIEWACCNO", "VIEWMOVES", "VIEWMP", "VIEWMPNO", "VIEWMTDNO" };

    private readonly IReadOnlySet<string> _changeTransactions = new HashSet<string> { "BLKMERGEGA", "BULKCOR", "CHGSETMTH", "CLSDWNMP", "CONNECTMP", "CREATEMP", "CREATESMP", "CREHISTMP", "CREMETER", "HTXCOR", "LNKCHLDMP", "MANCOR", "STPMETER", "ULNKCHLDMP", "UPDATESMP", "UPDHISTMP", "UPDMETER", "UPDPRDOBL", "XCONNECTMP", "MSTDATSBM" };

    private readonly IReadOnlySet<string> _unhandledTransactions = new HashSet<string> { "BLKBANKBS", "BULKCOR", "HTXCOR", "INCCHGSUP", "INCMOVEAUT", "INCMOVEIN", "INCMOVEMAN", "MANCOR" };

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

    private (bool Imported, string Message) TryImportTransaction(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint)
    {
        var currentTransactionType = importedTransaction.transaction_type.TrimEnd();

        if (_ignoredTransactions.Contains(currentTransactionType))
            return (true, string.Empty);

        var newMpPeriod = CreateMeteringPointPeriod(importedTransaction, meteringPoint, currentTransactionType);

        if (newMpPeriod.Type is not "Production" and not "Consumption")
            return (true, string.Empty);

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
            {
                // TODO: This does happen.
                return (false, "No balance_supplier_id for MOVEINES");
            }

            activeCommercialRelation.EnergySupplier = importedTransaction.balance_supplier_id.TrimEnd();
            activeCommercialRelation.ClientId = Guid.NewGuid();
            applyNewCommercialRelation = true;
        }

        if (currentTransactionType is "CHANGESUP" or "CHGSUPSHRT" or "MANCHGSUP")
        {
            if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id))
                throw new InvalidOperationException($"Missing balance_supplier_id for imported transaction id: {importedTransaction.Id}.");

            // TODO: CHANGESUP without customer is possible.
            var previousCommercialRelation = meteringPoint.CommercialRelations
                .SingleOrDefault(cr => cr.EndDate > importedTransaction.valid_from_date);

            activeCommercialRelation.EnergySupplier = importedTransaction.balance_supplier_id.TrimEnd();
            activeCommercialRelation.ClientId = previousCommercialRelation?.ClientId;
            applyNewCommercialRelation = true;
        }

        if (currentTransactionType is "MOVEOUTES")
        {
            if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id))
            {
                // TODO: This does happen.
                return (false, "No balance_supplier_id for MOVEOUTES");
            }

            activeCommercialRelation.EnergySupplier = importedTransaction.balance_supplier_id.TrimEnd();
            activeCommercialRelation.ClientId = null;
            applyNewCommercialRelation = true;
        }

        if (currentTransactionType is "ENDSUPPLY")
        {
            activeCommercialRelation.EnergySupplier = string.Empty;
            activeCommercialRelation.ClientId = null;
            applyNewCommercialRelation = true;
        }

        if (applyNewCommercialRelation)
        {
            var previousCommercialRelation = meteringPoint.CommercialRelations
                .SingleOrDefault(cr => cr.EndDate > importedTransaction.valid_from_date);

            if (previousCommercialRelation != null)
            {
                previousCommercialRelation.EndDate = importedTransaction.valid_from_date;
            }

            meteringPoint.CommercialRelations.Add(activeCommercialRelation);
        }
        else
        {
            if (_unhandledTransactions.Contains(currentTransactionType))
                return (false, $"Unhandled transaction type {currentTransactionType}");

            // TODO: Not matching Miro. Verify!
            activeCommercialRelation = meteringPoint.CommercialRelations
                .SingleOrDefault(cr => cr.EndDate > importedTransaction.valid_from_date);

            if (activeCommercialRelation == null)
                return (true, string.Empty);
        }

        var newEnergySupplyPeriod = new EnergySupplyPeriodEntity
        {
            ValidFrom = importedTransaction.valid_from_date,
            ValidTo = DateTimeOffset.MaxValue,
            CreatedAt = importedTransaction.dh2_created,
            BusinessTransactionDosId = importedTransaction.btd_trans_doss_id,
            WebAccessCode = importedTransaction.web_access_code?.TrimEnd() ?? string.Empty, // TODO: This is probably wrong.
            EnergySupplier = importedTransaction.balance_supplier_id?.TrimEnd() ?? "TODO: What?", // TODO: Fallback is sus.
        };

        // TODO: This is not verified.
        if (importedTransaction.first_consumer_party_name != null)
        {
            var contact = new ContactEntity
            {
                RelationType = "NoContact",
                DisponentName = importedTransaction.first_consumer_party_name.TrimEnd(),
                Cpr = importedTransaction.first_consumer_cpr?.TrimEnd(),
                Cvr = importedTransaction.consumer_cvr?.TrimEnd(),
                IsProtectedName = importedTransaction.protected_name ?? false,
            };

            if (importedTransaction.contact_1_contact_name1 != null)
            {
                contact.RelationType = "Contact1";
                contact.ContactName = importedTransaction.contact_1_contact_name1?.TrimEnd();
                contact.Email = importedTransaction.contact_1_email_address?.TrimEnd();
                contact.Phone = importedTransaction.contact_1_phone_number?.TrimEnd();
                contact.Mobile = importedTransaction.contact_1_mobile_number?.TrimEnd();
                contact.ContactAddress = new ContactAddressEntity
                {
                    IsProtectedAddress = importedTransaction.contact_1_protected_address ?? false,
                    Attention = importedTransaction.contact_1_attention?.TrimEnd(),
                    StreetCode = importedTransaction.contact_1_street_code?.TrimEnd(),
                    StreetName = importedTransaction.contact_1_street_name?.TrimEnd() ?? string.Empty,
                    BuildingNumber = importedTransaction.contact_1_building_number?.TrimEnd() ?? string.Empty,
                    CityName = importedTransaction.contact_1_city_name?.TrimEnd() ?? string.Empty,
                    CitySubdivisionName = importedTransaction.contact_1_city_subdivision_name?.TrimEnd(),
                    DarReference = importedTransaction.contact_1_dar_reference != null
                        ? Guid.Parse(importedTransaction.contact_1_dar_reference)
                        : null,
                    CountryCode = importedTransaction.contact_1_country_name?.TrimEnd() ?? string.Empty,
                    Floor = importedTransaction.contact_1_floor_id?.TrimEnd(),
                    Room = importedTransaction.contact_1_room_id?.TrimEnd(),
                    PostCode = importedTransaction.contact_1_postcode?.TrimEnd() ?? string.Empty,
                    MunicipalityCode = importedTransaction.contact_1_municipality_code?.TrimEnd(),
                };
            }

            newEnergySupplyPeriod.Contacts.Add(contact);

            if (importedTransaction.contact_4_contact_name1 != null)
            {
                var contact4 = new ContactEntity
                {
                    RelationType = "Contact4",
                    DisponentName = contact.DisponentName,
                    Cpr = contact.Cpr,
                    Cvr = contact.Cvr,
                    IsProtectedName = contact.IsProtectedName,
                    ContactName = importedTransaction.contact_4_contact_name1?.TrimEnd(),
                    Email = importedTransaction.contact_4_email_address?.TrimEnd(),
                    Phone = importedTransaction.contact_4_phone_number?.TrimEnd(),
                    Mobile = importedTransaction.contact_4_mobile_number?.TrimEnd(),
                    ContactAddress = new ContactAddressEntity
                    {
                        IsProtectedAddress = importedTransaction.contact_4_protected_address ?? false,
                        Attention = importedTransaction.contact_4_attention?.TrimEnd(),
                        StreetCode = importedTransaction.contact_4_street_code?.TrimEnd(),
                        StreetName = importedTransaction.contact_4_street_name?.TrimEnd() ?? string.Empty,
                        BuildingNumber = importedTransaction.contact_4_building_number?.TrimEnd() ?? string.Empty,
                        CityName = importedTransaction.contact_4_city_name?.TrimEnd() ?? string.Empty,
                        CitySubdivisionName = importedTransaction.contact_4_city_subdivision_name?.TrimEnd(),
                        DarReference = importedTransaction.contact_4_dar_reference != null
                            ? Guid.Parse(importedTransaction.contact_4_dar_reference)
                            : null,
                        CountryCode = importedTransaction.contact_4_country_name?.TrimEnd() ?? string.Empty,
                        Floor = importedTransaction.contact_4_floor_id?.TrimEnd(),
                        Room = importedTransaction.contact_4_room_id?.TrimEnd(),
                        PostCode = importedTransaction.contact_4_postcode?.TrimEnd() ?? string.Empty,
                        MunicipalityCode = importedTransaction.contact_4_municipality_code?.TrimEnd(),
                    }
                };

                newEnergySupplyPeriod.Contacts.Add(contact4);
            }
        }

        // TODO: This is not verified.
        if (importedTransaction.second_consumer_party_name != null)
        {
            var contact = new ContactEntity
            {
                RelationType = "Secondary",
                DisponentName = importedTransaction.second_consumer_party_name.TrimEnd(),
                Cpr = importedTransaction.second_consumer_cpr?.TrimEnd(),
                IsProtectedName = importedTransaction.protected_name ?? false,
            };

            newEnergySupplyPeriod.Contacts.Add(contact);
        }

        // TODO: Not matching Miro. Verify!
        var overlappingEnergySupplyPeriod = activeCommercialRelation.EnergySupplyPeriods
            .SingleOrDefault(esp => esp.RetiredBy == null && esp.ValidTo > importedTransaction.valid_from_date);

        if (overlappingEnergySupplyPeriod == null)
        {
            activeCommercialRelation.EnergySupplyPeriods.Add(newEnergySupplyPeriod);
            return (true, string.Empty);
        }

        var closedOverlappingEnergySupplyPeriod = new EnergySupplyPeriodEntity
        {
            ValidTo = importedTransaction.valid_from_date,
            ValidFrom = overlappingEnergySupplyPeriod.ValidFrom,
            CreatedAt = overlappingEnergySupplyPeriod.CreatedAt,
            BusinessTransactionDosId = overlappingEnergySupplyPeriod.BusinessTransactionDosId,
            WebAccessCode = overlappingEnergySupplyPeriod.WebAccessCode,
            EnergySupplier = overlappingEnergySupplyPeriod.EnergySupplier,
        };

        activeCommercialRelation.EnergySupplyPeriods.Add(closedOverlappingEnergySupplyPeriod);

        overlappingEnergySupplyPeriod.RetiredBy = closedOverlappingEnergySupplyPeriod;
        overlappingEnergySupplyPeriod.RetiredAt = importedTransaction.dh2_created; // TODO: Different from example.

        return (true, string.Empty);
    }

    private MeteringPointPeriodEntity CreateMeteringPointPeriod(ImportedTransactionEntity importedTransaction, MeteringPointEntity meteringPoint, string currentTransactionType)
    {
        var newMpPeriod = MeteringPointPeriodFactory.CreateMeteringPointPeriod(importedTransaction);

        if (_changeTransactions.Contains(currentTransactionType))
        {
            var currentlyActiveMeteringPointPeriod = meteringPoint.MeteringPointPeriods
                .Where(p => p.RetiredBy == null && p.ValidFrom <= importedTransaction.valid_from_date)
                .MaxBy(x => x.ValidFrom);

            if (currentlyActiveMeteringPointPeriod != null)
            {
                if (currentlyActiveMeteringPointPeriod.ValidFrom == importedTransaction.valid_from_date)
                {
                    if (currentlyActiveMeteringPointPeriod.ValidTo == DateTimeOffset.MaxValue)
                    {
                        newMpPeriod.ValidTo = DateTimeOffset.MaxValue;
                    }
                    else
                    {
                        newMpPeriod.ValidTo = meteringPoint.MeteringPointPeriods
                            .Where(p => p.ValidFrom > newMpPeriod.ValidFrom && p.RetiredBy == null)
                            .Min(p => p.ValidFrom);

                        currentlyActiveMeteringPointPeriod.RetiredAt = DateTimeOffset.UtcNow;
                        currentlyActiveMeteringPointPeriod.RetiredBy = newMpPeriod;
                    }
                }
                else
                {
                    var copy = MeteringPointPeriodFactory.CopyMeteringPointPeriod(currentlyActiveMeteringPointPeriod);

                    copy.ValidTo = importedTransaction.valid_from_date;

                    meteringPoint.MeteringPointPeriods.Add(copy);

                    currentlyActiveMeteringPointPeriod.RetiredAt = DateTimeOffset.UtcNow;
                    currentlyActiveMeteringPointPeriod.RetiredBy = copy;

                    newMpPeriod.ValidTo = currentlyActiveMeteringPointPeriod.ValidTo == DateTimeOffset.MaxValue
                        ? DateTimeOffset.MaxValue
                        : meteringPoint.MeteringPointPeriods
                            .Where(p => p.ValidFrom > newMpPeriod.ValidFrom && p.RetiredBy == null)
                            .Min(p => p.ValidFrom);
                }
            }

            meteringPoint.MeteringPointPeriods.Add(newMpPeriod);
        }
        else
        {
            // TODO: When transaction type is not in changeTransactions. This is not modelled.
        }

        return newMpPeriod;
    }
}
