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
using System.Globalization;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Helpers;

public static class ScheduledMeterReadingMonthHelper
{
    public static int? ConvertToSingleMonth(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (input.Length != 4)
        {
            throw new ArgumentException("Date for scheduled meter reading month is invalid", nameof(input));
        }

        var (month, day) = (int.Parse(input[..2], CultureInfo.InvariantCulture), int.Parse(input[2..], CultureInfo.InvariantCulture));
        switch (day)
        {
            case >= 1 and < 28:
                return month;
            default:
                {
                    if (month != 2 && DateTime.DaysInMonth(1900, month) == day)
                    {
                        // The year is irrelevant, as all months except february have the same amount of days regardless of leap years
                        return month + 1;
                    }
                    else
                    {
                        return month == 2 && day is 28 or 29
                            ? month + 1
                            : throw new ArgumentException($"Date for scheduled meter reading month is invalid month: {month} | day: {day} ", nameof(input));
                    }
                }
        }
    }
}
