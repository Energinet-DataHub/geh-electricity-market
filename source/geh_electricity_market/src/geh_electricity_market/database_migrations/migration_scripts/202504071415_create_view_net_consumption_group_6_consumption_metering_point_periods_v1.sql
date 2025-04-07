CREATE VIEW {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_consumption_metering_point_periods_v1
AS SELECT (
    metering_point_id COMMENT 'GSRN Number',
    has_electrical_heating COMMENT 'States whether the metering point has electrical heating in the period.',
    settlement_month COMMENT 'The settlement month. 1 is January, 12 is December.',
    period_from_date COMMENT 'UTC time. The period start date.',
    period_to_date COMMENT 'UTC time. The period end date.',
    move_in COMMENT 'New customer moves in (true) all other changes (false)'
) FROM {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_consumption_metering_point_periods