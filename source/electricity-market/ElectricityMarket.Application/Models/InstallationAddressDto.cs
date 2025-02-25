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

public sealed record InstallationAddressDto(
    long Id,
    string StreetCode, // location_street_code
    string StreetName, // location_street_name
    string BuildingNumber, // location_building_number
    string CityName, // location_city_name
    string? CitySubDivisionName, // location_city_subdivision_name
    Guid? DarReference, // location_dar_reference
    string CountryCode, // location_country_name
    string? Floor, // location_floor_id
    string? Room, // location_room_id
    string PostCode, // location_postcode
    string? MunicipalityCode, // location_municipality_code
    string? LocationDescription); // location_location_description
