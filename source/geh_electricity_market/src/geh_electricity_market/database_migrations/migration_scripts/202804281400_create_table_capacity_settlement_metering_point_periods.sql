CREATE TABLE {catalog_name}.electricity_market_internal.capacity_settlement_metering_point_periods (
    metering_point_id STRING NOT NULL,
    period_from_date TIMESTAMP NOT NULL,
    period_to_date TIMESTAMP,
    child_metering_point_id STRING NOT NULL,
    child_period_from_date TIMESTAMP NOT NULL,
    child_period_to_date TIMESTAMP
)
USING DELTA
