CREATE VIEW {catalog_name}.electricity_market_measurements_input.electrical_heating_consumption_metering_point_periods_v1 
AS SELECT (
    metering_point_id,
    has_electrical_heating,
    net_settlement_group,
    settlement_month,
    period_from_date,
    period_to_date
) FROM {catalog_name}.electricity_market_measurements_input.electrical_heating_consumption_metering_point_periods