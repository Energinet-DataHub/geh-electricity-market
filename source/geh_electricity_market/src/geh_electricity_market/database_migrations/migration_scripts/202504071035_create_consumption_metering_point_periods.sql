CREATE TABLE IF NOT EXISTS {catalog_name}.electricity_market_input.consumption_metering_point_periods (
    metering_point_id STRING NOT NULL,
    has_electrical_heating BOOLEAN NOT NULL,
    net_settlement_group INTEGER,
    settlement_month INTEGER NOT NULL,
    period_from_date DATETIME NOT NULL,
    period_to_date DATETIME,
);