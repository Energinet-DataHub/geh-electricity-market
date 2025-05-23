﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public static class CommercialRelationFactory
{
    public static CommercialRelationEntity CreateCommercialRelation(ImportedTransactionEntity importedTransaction)
    {
        ArgumentNullException.ThrowIfNull(importedTransaction);

        if (string.IsNullOrWhiteSpace(importedTransaction.balance_supplier_id))
        {
            throw new ArgumentException($"{nameof(importedTransaction.balance_supplier_id)} is required", nameof(importedTransaction));
        }

        var commercialRelation = new CommercialRelationEntity
        {
            StartDate = importedTransaction.valid_from_date,
            EndDate = DateTimeOffset.MaxValue,
            EnergySupplier = importedTransaction.balance_supplier_id.TrimEnd(),
            ModifiedAt = importedTransaction.dh2_created,
            ClientId = Guid.NewGuid(),
        };

        return commercialRelation;
    }

    public static EnergySupplyPeriodEntity CreateEnergySupplyPeriodEntity(ImportedTransactionEntity importedTransaction)
    {
        ArgumentNullException.ThrowIfNull(importedTransaction);

        var energySupplyPeriodEntity = new EnergySupplyPeriodEntity
        {
            ValidFrom = importedTransaction.valid_from_date,
            ValidTo = DateTimeOffset.MaxValue,
            CreatedAt = importedTransaction.dh2_created,
            BusinessTransactionDosId = importedTransaction.btd_trans_doss_id,
            WebAccessCode = importedTransaction.web_access_code?.TrimEnd() ?? string.Empty,
            EnergySupplier = importedTransaction.balance_supplier_id?.TrimEnd() ?? string.Empty,
            TransactionType = importedTransaction.transaction_type.TrimEnd(),
        };

        if (importedTransaction.first_consumer_party_name != null)
        {
            var contact = new ContactEntity
            {
                RelationType = "NoContact",
                DisponentName = importedTransaction.first_consumer_party_name.TrimEnd(),
                Cpr = importedTransaction.first_consumer_cpr?.TrimEnd(),
                Cvr = importedTransaction.consumer_cvr?.TrimEnd(),
                IsProtectedName = importedTransaction.protected_name ?? false,
                ContactAddresses =
                {
                    new ContactAddressEntity
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
                        PostBox = importedTransaction.contact_1_post_box?.TrimEnd(),
                        MunicipalityCode = importedTransaction.contact_1_municipality_code?.TrimEnd(),
                    }
                }
            };

            contact.RelationType = "Contact1";
            contact.ContactName = importedTransaction.contact_1_contact_name1?.TrimEnd();
            contact.Email = importedTransaction.contact_1_email_address?.TrimEnd();
            contact.Phone = importedTransaction.contact_1_phone_number?.TrimEnd();
            contact.Mobile = importedTransaction.contact_1_mobile_number?.TrimEnd();

            energySupplyPeriodEntity.Contacts.Add(contact);

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
                ContactAddresses =
                {
                    new ContactAddressEntity
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
                        PostBox = importedTransaction.contact_4_post_box?.TrimEnd(),
                        MunicipalityCode = importedTransaction.contact_4_municipality_code?.TrimEnd(),
                    }
                },
            };

            energySupplyPeriodEntity.Contacts.Add(contact4);
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
                ContactAddresses = { new ContactAddressEntity() }
            };

            energySupplyPeriodEntity.Contacts.Add(contact);
        }

        return energySupplyPeriodEntity;
    }

    public static EnergySupplyPeriodEntity CopyEnergySupplyPeriod(EnergySupplyPeriodEntity energySupplyPeriodEntity)
    {
        ArgumentNullException.ThrowIfNull(energySupplyPeriodEntity);

        var copy = new EnergySupplyPeriodEntity
        {
            ValidFrom = energySupplyPeriodEntity.ValidFrom,
            ValidTo = energySupplyPeriodEntity.ValidTo,
            CreatedAt = energySupplyPeriodEntity.CreatedAt,
            BusinessTransactionDosId = energySupplyPeriodEntity.BusinessTransactionDosId,
            WebAccessCode = energySupplyPeriodEntity.WebAccessCode,
            EnergySupplier = energySupplyPeriodEntity.EnergySupplier,
            TransactionType = energySupplyPeriodEntity.TransactionType,
        };

        foreach (var contact in energySupplyPeriodEntity.Contacts)
        {
            var contactCopy = new ContactEntity
            {
                RelationType = contact.RelationType,
                DisponentName = contact.DisponentName,
                Cpr = contact.Cpr,
                Cvr = contact.Cvr,
                IsProtectedName = contact.IsProtectedName,
                ContactName = contact.ContactName,
                Email = contact.Email,
                Phone = contact.Phone,
                Mobile = contact.Mobile,
                ContactAddresses =
                {
                    new ContactAddressEntity
                    {
                        IsProtectedAddress = contact.ContactAddresses.Single().IsProtectedAddress,
                        Attention = contact.ContactAddresses.Single().Attention,
                        StreetCode = contact.ContactAddresses.Single().StreetCode,
                        StreetName = contact.ContactAddresses.Single().StreetName,
                        BuildingNumber = contact.ContactAddresses.Single().BuildingNumber,
                        CityName = contact.ContactAddresses.Single().CityName,
                        CitySubdivisionName = contact.ContactAddresses.Single().CitySubdivisionName,
                        DarReference = contact.ContactAddresses.Single().DarReference,
                        CountryCode = contact.ContactAddresses.Single().CountryCode,
                        Floor = contact.ContactAddresses.Single().Floor,
                        Room = contact.ContactAddresses.Single().Room,
                        PostCode = contact.ContactAddresses.Single().PostCode,
                        PostBox = contact.ContactAddresses.Single().PostBox,
                        MunicipalityCode = contact.ContactAddresses.Single().MunicipalityCode
                    }
                }
            };

            copy.Contacts.Add(contactCopy);
        }

        return copy;
    }
}
