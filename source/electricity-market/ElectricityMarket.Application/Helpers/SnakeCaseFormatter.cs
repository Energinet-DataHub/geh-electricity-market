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

using System.Globalization;
using System.Text.RegularExpressions;

namespace Energinet.DataHub.ElectricityMarket.Application.Helpers;

public class SnakeCaseFormatter
{
    private readonly string _regex = "(?!^)[_ ]*([A-Z][a-z]*)";

    public string ToSnakeCase(string name)
    {
        return Regex.Replace(name, _regex, "_$1").ToLower(CultureInfo.CurrentCulture);
    }
}
