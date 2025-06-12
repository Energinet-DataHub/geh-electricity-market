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

namespace Energinet.DataHub.ElectricityMarket.Application.Services;

public static class IEnumerableCombineExtension
{
    public static IEnumerable<T> Combine<T>(this IEnumerable<T> input, Func<T, T, bool> combineCriteria, Func<T, T, T> combiner)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(combineCriteria);
        ArgumentNullException.ThrowIfNull(combiner);

        var result = input.ToList();

        if (result.Count < 2)
        {
            return result;
        }

        for (var i = 0; i < result.Count - 1; i++)
        {
            if (combineCriteria(result[i], result[i + 1]))
            {
                // Combine i and i + 1
                result[i] = combiner(result[i], result[i + 1]);
                result.RemoveAt(i + 1);

                // Single interval left
                if (result.Count == 1)
                {
                    break;
                }

                // Consider current item again
                i = i - 1;
            }
        }

        return result;
    }
}
