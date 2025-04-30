CREATE TABLE {catalog_name}.electricity_market_internal.electrical_heating_consumption_metering_point_periods (
    metering_point_id STRING NOT NULL COMMENT 'GSRN Number',
    period_from_date TIMESTAMP NOT NULL COMMENT 'UTC time. The period start date.',
    period_to_date TIMESTAMP COMMENT 'UTC time. The period end date.',
    has_electrical_heating BOOLEAN NOT NULL COMMENT 'States whether the metering point has electrical heating in the period.',
    net_settlement_group INTEGER COMMENT 'The net settlement group of the metering point.',
    settlement_month INTEGER NOT NULL COMMENT 'The settlement month. 1 is January, 12 is December.'
)
USING DELTA
-- CLUSTER BY metering_point_id, period_from_date, period_to_date;
-- TODO: What to cluster by? --> Bjarke, JMK, Jonas
