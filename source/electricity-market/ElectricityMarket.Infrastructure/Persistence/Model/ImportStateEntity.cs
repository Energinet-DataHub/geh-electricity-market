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

public sealed class ImportStateEntity
{
    public const int Off = 0;
    public const int Scheduled = 1;
    public const int BulkImport = 2;
    public const int StreamingImport = 3;
    public const int GoldenStreaming = 4;

    public int Id { get; set; }

    public int Mode { get; set; }

    public long Offset { get; set; }
}
