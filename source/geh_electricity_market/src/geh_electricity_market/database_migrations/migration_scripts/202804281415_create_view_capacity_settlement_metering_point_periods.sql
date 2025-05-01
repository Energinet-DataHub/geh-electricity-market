CREATE VIEW {catalog_name}.electricity_market_measurements_input.capacity_settlement_metering_point_periods_v1 AS
SELECT
    metering_point_id,
    period_from_date,
    period_to_date,
    child_metering_point_id,
    child_period_from_date,
    child_period_to_date
FROM {catalog_name}.electricity_market_internal.capacity_settlement_metering_point_periods
