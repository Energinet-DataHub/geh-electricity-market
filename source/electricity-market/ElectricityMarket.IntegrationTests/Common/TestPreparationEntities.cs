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
using System.Globalization;
using System.Linq;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;
using Energinet.DataHub.ElectricityMarket.Integration;
using NodaTime;
using NodaTime.Extensions;

namespace Energinet.DataHub.ElectricityMarket.IntegrationTests.Common;

public static class TestPreparationEntities
{
    // Grid Areas starts at 10 because '003' and '007' are reserved and should not "occur" randomly as tests are run.
    private static int _gridAreaCount = 10;

    public static MeteringPointEntity ValidMeteringPoint => new()
    {
        Identification = new string(Enumerable.Range(0, 18).Select(_ => (char)('0' + Random.Shared.Next(10))).ToArray())
    };

    public static MeteringPointPeriodEntity ValidMeteringPointPeriod => new()
    {
        ValidFrom = new DateTimeOffset(2020, 12, 31, 23, 0, 0, TimeSpan.Zero).ToInstant(),
        ValidTo = new DateTimeOffset(9999, 12, 31, 23, 0, 0, TimeSpan.Zero).ToInstant(),
        CreatedAt = SystemClock.Instance.GetCurrentInstant(),
        GridAreaCode = (_gridAreaCount++ % 1000).ToString(CultureInfo.InvariantCulture).PadLeft(3, '0'),
        OwnedBy = "4672928796219",
        ConnectionState = ConnectionState.Connected.ToString(),
        Type = MeteringPointType.Consumption.ToString(),
        SubType = MeteringPointSubType.Physical.ToString(),
        Resolution = "PT15M",
        Unit = MeasureUnit.kWh.ToString(),
        ProductId = ProductId.PowerActive.ToString(),
        ScheduledMeterReadingMonth = 1
    };

    public static CommercialRelationEntity ValidCommercialRelation => new()
    {
        EnergySupplier = "2334379799509",
        StartDate = new DateTimeOffset(2020, 12, 31, 23, 0, 0, TimeSpan.Zero).ToInstant(),
        EndDate = new DateTimeOffset(9999, 12, 31, 23, 0, 0, TimeSpan.Zero).ToInstant(),
        ModifiedAt = SystemClock.Instance.GetCurrentInstant()
    };

    public static T Patch<T>(this T entity, Action<T> action)
    {
        action(entity);
        return entity;
    }
}
