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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services.Import;

public sealed class RelationalModelPrinter : IRelationalModelPrinter
{
    public Task<string> PrintAsync(IEnumerable<IList<MeteringPointEntity>> relationalModelBatches, IEnumerable<IList<QuarantinedMeteringPointEntity>> quarantined, CultureInfo cultureInfo)
    {
        var sb = new StringBuilder();

        var meteringPointEntities = relationalModelBatches.SelectMany(x => x).ToList();

        sb.Append(
            PrettyPrintObjects(meteringPointEntities.ToList(), cultureInfo));
        sb.Append(
            PrettyPrintObjects(
                meteringPointEntities.Select(x => x.MeteringPointPeriods).SelectMany(x => x).ToList(),
                cultureInfo,
                orderingOverrides:
                [
                    (PropertiesToMove: [x => x.TransactionType, x => x.EffectuationDate, x => x.ConnectionState],
                        InsertAfter: x => x.ConnectionType),
                ]));
        sb.Append(
            PrettyPrintObjects(meteringPointEntities.Select(x => x.MeteringPointPeriods).SelectMany(x => x.Select(y => y.InstallationAddress)).ToList(), cultureInfo));
        sb.Append(
            PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x).ToList(), cultureInfo));
        sb.Append(
            PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.EnergySupplyPeriods)).ToList(), cultureInfo));
        sb.Append(
            PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.EnergySupplyPeriods).SelectMany(z => z.Contacts)).ToList(), cultureInfo));
        sb.Append(
            PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.EnergySupplyPeriods).SelectMany(z => z.Contacts.Select(i => i.ContactAddress))).Where(j => j is not null).ToList(), cultureInfo));
        sb.Append(
            PrettyPrintObjects(meteringPointEntities.Select(x => x.CommercialRelations).SelectMany(x => x.SelectMany(y => y.ElectricalHeatingPeriods)).ToList(), cultureInfo));

        var quarantinedMeteringPoints = quarantined.SelectMany(x => x).ToList();
        sb.Append(
            PrettyPrintObjects(quarantinedMeteringPoints.ToList(), cultureInfo));

        return Task.FromResult(sb.ToString());
    }

    private static string PrettyPrintObjects<T>(IList<T> items, CultureInfo cultureInfo, (Expression<Func<T, object?>>[] PropertiesToMove, Expression<Func<T, object?>> InsertAfter)[]? orderingOverrides = null)
    {
        if (items.Count == 0) return string.Empty;

        var ordering = GetOrdering(orderingOverrides ?? []);

        var sb = new StringBuilder();
        sb.AppendLine(items[0]!.GetType().Name.Replace("Entity", string.Empty, StringComparison.InvariantCulture));
        var properties = items.First()!.GetType()
            .GetProperties()
            .Where(p => !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string))
            .Where(p => p.PropertyType.IsPrimitive || p.PropertyType.IsValueType || p.PropertyType == typeof(string))
            .OrderBy(p => ordering.IndexOf(p.Name))
            .ToArray();

        var columnWidths = properties
            .Select(p => items
                .Select(i => p.GetValue(i)?.ToString()?.Length ?? 0)
                .Prepend(p.Name.Length)
                .Max())
            .ToArray();

        var separator = "+-" + string.Join("-+-", columnWidths.Select(w => new string('-', w))) + "-+";
        sb.AppendLine(separator);

        var header = "| " + string.Join(" | ", properties.Select((p, i) => p.Name.PadRight(columnWidths[i]))) + " |";
        sb.AppendLine(header);
        sb.AppendLine(separator);

        foreach (var item in items)
        {
            var rowText = "| " + string.Join(" | ", properties
                .Select((p, i) =>
                {
                    var value = p.GetValue(item);
                    return value is DateTimeOffset dateTimeOffset
                        ? dateTimeOffset.ToString(cultureInfo).PadRight(columnWidths[i])
                        : value?.ToString()?.PadRight(columnWidths[i]) ?? string.Empty.PadRight(columnWidths[i]);
                })) + " |";
            sb.AppendLine(rowText);
        }

        sb.AppendLine(separator);

        var prettyPrintObjects = sb.ToString();

        return !string.IsNullOrWhiteSpace(prettyPrintObjects) ? prettyPrintObjects + "\n" : prettyPrintObjects;

        static List<string> GetOrdering((Expression<Func<T, object?>>[] PropertiesToMove, Expression<Func<T, object?>> InsertAfter)[] orderingOverrides)
        {
            var defaultOrder = typeof(T).GetProperties().Select(x => x.Name).ToList();

            foreach (var (propertiesToMove, insertAfter) in orderingOverrides)
            {
                var insertAfterPropertyName = GetExpressionName(insertAfter);

                for (var i = 0; i < propertiesToMove.Length; i++)
                {
                    var propertyToMoveName = GetExpressionName(propertiesToMove[i]);
                    defaultOrder.Remove(propertyToMoveName);
                    defaultOrder.Insert(defaultOrder.IndexOf(insertAfterPropertyName) + 1 + i, propertyToMoveName);
                }
            }

            return defaultOrder;

            static string GetExpressionName(Expression<Func<T, object?>> expression)
            {
                return expression.Body switch
                {
                    MemberExpression memberExpression => memberExpression.Member.Name,
                    UnaryExpression { Operand: MemberExpression } unaryExpression => ((MemberExpression)unaryExpression.Operand).Member.Name,
                    _ => throw new InvalidOperationException(),
                };
            }
        }
    }
}
