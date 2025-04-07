CREATE VIEW {catalog_name}.electricity_market_measurements_input.electrical_heating_consumption_metering_point_periods_v1 
AS SELECT (
    metering_point_id COMMENT 'GSRN Number',
    period_from_date COMMENT 'UTC time. The period start date.',
    period_to_date COMMENT 'UTC time. The period end date.',
    has_electrical_heating COMMENT 'States whether the metering point has electrical heating in the period.',
    net_settlement_group COMMENT 'The net settlement group of the metering point.',
    settlement_month COMMENT 'The settlement month. 1 is January, 12 is December.'
) FROM {catalog_name}.electricity_market_measurements_input.electrical_heating_consumption_metering_point_periods