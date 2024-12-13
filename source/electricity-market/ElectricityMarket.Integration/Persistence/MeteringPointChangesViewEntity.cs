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
using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.ElectricityMarket.Integration.Persistence;

internal sealed class MeteringPointChangesViewEntity
{
    [MaxLength(18)]
    public string Identification { get; internal set; } = null!;

    public DateTimeOffset ValidFrom { get; internal set; }

    public DateTimeOffset ValidTo { get; internal set; }

    [MaxLength(3)]
    public string GridAreaCode { get; internal set; } = null!;

    [MaxLength(16)]
    public string GridAccessProvider { get; internal set; } = null!;

    public string ConnectionState { get; internal set; } = null!;

    public string Type { get; internal set; } = null!;

    public string SubType { get; internal set; } = null!;

    [MaxLength(6)]
    public string Resolution { get; internal set; } = null!;

    public string Unit { get; internal set; } = null!;

    public string ProductId { get; internal set; } = null!;
}
