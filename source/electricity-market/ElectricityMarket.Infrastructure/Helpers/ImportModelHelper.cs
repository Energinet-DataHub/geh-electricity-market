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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Energinet.DataHub.ElectricityMarket.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Helpers;

public static class ImportModelHelper
{
    public static readonly IEnumerable<KeyValuePair<string, Action<object?, ImportedTransactionEntity>>> ImportFields =
    [
        MapProperty(t => t.metering_point_id),
        MapProperty(t => t.valid_from_date),
        MapProperty(t => t.valid_to_date, DateTimeOffset.MaxValue),
        MapProperty(t => t.dh2_created),
        MapProperty(t => t.metering_grid_area_id),
        MapProperty(t => t.metering_point_state_id),
        MapProperty(t => t.btd_trans_doss_id, -1),
        MapProperty(t => t.parent_metering_point_id),
        MapProperty(t => t.type_of_mp),
        MapProperty(t => t.sub_type_of_mp),
        MapProperty(t => t.physical_status_of_mp),
        MapProperty(t => t.web_access_code),
        MapProperty(t => t.balance_supplier_id),
        MapProperty(t => t.transaction_type, string.Empty),
        MapProperty(t => t.meter_reading_occurrence),
        MapProperty(t => t.mp_connection_type),
        MapProperty(t => t.disconnection_type),
        MapProperty(t => t.product),
        MapProperty(t => t.product_obligation),
        MapProperty(t => t.energy_timeseries_measure_unit),
        MapProperty(t => t.asset_type),
        MapProperty(t => t.fuel_type),
        MapProperty(t => t.mp_capacity),
        MapProperty(t => t.power_limit_kw),
        MapProperty(t => t.power_limit_a),
        MapProperty(t => t.meter_number),
        MapProperty(t => t.net_settlement_group),
        MapProperty(t => t.scheduled_meter_reading_date01),
        MapProperty(t => t.from_grid_area),
        MapProperty(t => t.to_grid_area),
        MapProperty(t => t.power_plant_gsrn),
        MapProperty(t => t.settlement_method),
        MapProperty(t => t.location_street_code),
        MapProperty(t => t.location_street_name),
        MapProperty(t => t.location_building_number),
        MapProperty(t => t.location_city_name),
        MapProperty(t => t.location_city_subdivision_name),
        MapProperty(t => t.location_dar_reference),
        MapProperty(t => t.location_mp_address_wash_instructions),
        MapProperty(t => t.location_country_name),
        MapProperty(t => t.location_floor_id),
        MapProperty(t => t.location_room_id),
        MapProperty(t => t.location_postcode),
        MapProperty(t => t.location_municipality_code),
        MapProperty(t => t.location_location_description),
        MapProperty(t => t.first_consumer_party_name),
        MapProperty(t => t.first_consumer_cpr),
        MapProperty(t => t.second_consumer_party_name),
        MapProperty(t => t.second_consumer_cpr),
        MapProperty(t => t.consumer_cvr),
        MapProperty(t => t.protected_name),
        MapProperty(t => t.contact_1_contact_name1),
        MapProperty(t => t.contact_1_protected_address),
        MapProperty(t => t.contact_1_phone_number),
        MapProperty(t => t.contact_1_mobile_number),
        MapProperty(t => t.contact_1_email_address),
        MapProperty(t => t.contact_1_attention),
        MapProperty(t => t.contact_1_street_code),
        MapProperty(t => t.contact_1_street_name),
        MapProperty(t => t.contact_1_building_number),
        MapProperty(t => t.contact_1_postcode),
        MapProperty(t => t.contact_1_city_name),
        MapProperty(t => t.contact_1_city_subdivision_name),
        MapProperty(t => t.contact_1_dar_reference),
        MapProperty(t => t.contact_1_country_name),
        MapProperty(t => t.contact_1_floor_id),
        MapProperty(t => t.contact_1_room_id),
        MapProperty(t => t.contact_1_post_box),
        MapProperty(t => t.contact_1_municipality_code),
        MapProperty(t => t.contact_4_contact_name1),
        MapProperty(t => t.contact_4_protected_address),
        MapProperty(t => t.contact_4_phone_number),
        MapProperty(t => t.contact_4_mobile_number),
        MapProperty(t => t.contact_4_email_address),
        MapProperty(t => t.contact_4_attention),
        MapProperty(t => t.contact_4_street_code),
        MapProperty(t => t.contact_4_street_name),
        MapProperty(t => t.contact_4_building_number),
        MapProperty(t => t.contact_4_postcode),
        MapProperty(t => t.contact_4_city_name),
        MapProperty(t => t.contact_4_city_subdivision_name),
        MapProperty(t => t.contact_4_dar_reference),
        MapProperty(t => t.contact_4_country_name),
        MapProperty(t => t.contact_4_floor_id),
        MapProperty(t => t.contact_4_room_id),
        MapProperty(t => t.contact_4_post_box),
        MapProperty(t => t.contact_4_municipality_code),
        MapProperty(t => t.dossier_status),
    ];

    public static Func<T, object> GetValueGetter<T>(this PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        if (typeof(T) != propertyInfo.DeclaringType)
            throw new ArgumentException("not allowed");

        var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
        var property = Expression.Property(instance, propertyInfo);
        var convert = Expression.TypeAs(property, typeof(object));
        return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
    }

    private static KeyValuePair<string, Action<object?, ImportedTransactionEntity>> MapProperty<TProp>(
        Expression<Func<ImportedTransactionEntity, TProp>> propertyExpression,
        TProp? defaultValue = default)
    {
        if (propertyExpression.Body is not MemberExpression memberExpression)
            throw new ArgumentException("The expression must be a property access expression.", nameof(propertyExpression));

        if (memberExpression.Member is not PropertyInfo propertyInfo)
            throw new ArgumentException("The expression must point to a property.", nameof(propertyExpression));

        var target = Expression.Parameter(typeof(ImportedTransactionEntity), "target");
        var value = Expression.Parameter(typeof(object), "value");

        Expression convertedValue;

        if (!Equals(defaultValue, default(TProp)))
        {
            convertedValue = Expression.Condition(
                Expression.Equal(value, Expression.Constant(null)),
                Expression.Constant(defaultValue, propertyInfo.PropertyType),
                Expression.Convert(value, propertyInfo.PropertyType));
        }
        else
        {
            convertedValue = Expression.Convert(value, propertyInfo.PropertyType);
        }

        var body = Expression.Assign(Expression.Property(target, propertyInfo), convertedValue);
        var compiled = Expression.Lambda<Action<object?, ImportedTransactionEntity>>(body, value, target).Compile();

        return new KeyValuePair<string, Action<object?, ImportedTransactionEntity>>(propertyInfo.Name, compiled);
    }
}
