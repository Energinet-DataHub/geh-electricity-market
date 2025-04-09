CREATE VIEW {catalog_name}.electricity_market_measurements_input.missing_measurements_log_metering_point_periods_v1
AS SELECT (
    metering_point_id,
    grid_area_code,
    resolution,
    period_from_date,
    period_to_date
) FROM {catalog_name}.electricity_market_measurements_input.missing_measurements_log_metering_point_periods