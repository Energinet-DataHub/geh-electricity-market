CREATE TABLE IF NOT EXISTS {catalog_name}.electricity_market_measurements_input.electrical_heating_child_metering_points (
    metering_point_id STRING NOT NULL,
    metering_point_type STRING NOT NULL,
    metering_point_sub_type STRING NOT NULL,
    parent_metering_point_id STRING not NULL,
    coupled_date DATETIME NOT NULL,
    uncoupled_date DATETIME,
);