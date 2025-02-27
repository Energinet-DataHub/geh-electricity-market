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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

public sealed class ContactEntity
{
    public long Id { get; set; }
    public long EnergySupplyPeriodId { get; set; }

    public string RelationType { get; set; } = null!;
    public string DisponentName { get; set; } = null!;
    public string? Cpr { get; set; }
    public string? Cvr { get; set; }

    public bool IsProtectedName { get; set; }
    public long ContactAddressId { get; set; }
    public string? ContactName { get; set; } = null!;
    public string? Email { get; set; } = null!;
    public string? Phone { get; set; } = null!;
    public string? Mobile { get; set; } = null!;

    public ContactAddressEntity ContactAddress { get; set; } = null!;
}
