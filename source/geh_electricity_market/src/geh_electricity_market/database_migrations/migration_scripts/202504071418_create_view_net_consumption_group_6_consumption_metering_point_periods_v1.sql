CREATE VIEW {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_consumption_metering_point_periods_v1
AS SELECT (
    metering_point_id,
    has_electrical_heating,
    settlement_month,
    period_from_date,
    period_to_date,
    move_in
) FROM {catalog_name}.electricity_market_internal.net_consumption_group_6_consumption_metering_point_periods