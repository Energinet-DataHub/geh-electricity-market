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

namespace ElectricityMarket.WebAPI;

public static class SubsystemInformation
{
    public static Guid Id { get; } = Guid.Parse("1fc93427-e6fb-45db-bf92-b6efefe5aad9");
    public static string Name => "electricity-market";
}
