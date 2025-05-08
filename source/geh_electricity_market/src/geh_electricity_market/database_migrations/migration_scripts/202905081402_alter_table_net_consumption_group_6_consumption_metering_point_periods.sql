-- CREATE TABLE {catalog_name}.electricity_market_internal.net_consumption_group_6_consumption_metering_point_periods (
--     metering_point_id STRING NOT NULL COMMENT 'GSRN Number',
--     has_electrical_heating BOOLEAN NOT NULL COMMENT 'States whether the metering point has electrical heating in the period.',
--     settlement_month INTEGER NOT NULL COMMENT 'The settlement month. 1 is January, 12 is December.',
--     period_from_date TIMESTAMP NOT NULL COMMENT 'UTC time. The period start date.',
--     period_to_date TIMESTAMP COMMENT 'UTC time. The period end date.',
--     move_in BOOLEAN COMMENT 'New customer moves in (true) all other changes (false)'
-- )
-- USING DELTA

ALTER TABLE {catalog_name}.electricity_market_internal.net_consumption_group_6_consumption_metering_point_periods
CLUSTER BY (metering_point_id, period_from_date, period_to_date)
