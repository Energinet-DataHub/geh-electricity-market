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

namespace Energinet.DataHub.ElectricityMarket.Application.Models;

public sealed record CustomerContactDto(
    long Id,
    string Name, // contact_1_contact_name1
    bool IsProtectedAddress, // contact_1_protected_address
    string? Phone, // contact_1_phone_number
    string? Mobile, // contact_1_mobile_number
    string Email, // contact_1_email_address
    string? Attention, // contact_1_attention
    string StreetCode, // contact_1_street_code
    string StreetName, // contact_1_street_name
    string BuildingNumber, // contact_1_building_number
    string PostCode, // contact_1_postcode
    string CityName, // contact_1_city_name
    string? CitySubDivisionName, // contact_1_city_subdivision_name
    Guid? DarReference, // contact_1_dar_reference
    string CountryCode, // contact_1_country_name
    string? Floor, // contact_1_floor_id
    string? Room, // contact_1_room_id
    string? PostBox, // contact_1_post_box
    string? MunicipalityCode); // contact_1_municipality_code
