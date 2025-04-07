CREATE TABLE {catalog_name}.electricity_market_measurements_input.electrical_heating_consumption_metering_point_periods (
    metering_point_id STRING NOT NULL,
    period_from_date DATETIME NOT NULL,
    period_to_date DATETIME
    has_electrical_heating BOOLEAN NOT NULL,
    net_settlement_group INTEGER,
    settlement_month INTEGER NOT NULL,
) -- CLUSTER BY metering_point_id, period_from_date, period_to_date;
-- TODO: What to cluster by? --> Bjarke, JMK, Jonas