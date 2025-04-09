CREATE VIEW {catalog_name}.electricity_market_measurements_input.missing_measurements_log_metering_point_periods_v1
AS SELECT (
    metering_point_id COMMENT 'GSRN Number',
    grid_area_code COMMENT 'The code of the grid area that the metering point belongs to',
    resolution COMMENT 'Metering point resolution: PT1H/PT15M',
    period_from_date COMMENT 'UTC time. The period start date.',
    period_to_date COMMENT 'UTC time. The period end date.'
) FROM {catalog_name}.electricity_market_measurements_input.missing_measurements_log_metering_point_periods