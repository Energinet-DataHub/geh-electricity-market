CREATE TABLE {catalog_name}.electricity_market_measurements_input.electrical_heating_child_metering_points (
    parent_metering_point_id STRING not NULL,
    metering_point_id STRING NOT NULL,
    metering_point_type STRING NOT NULL,
    metering_point_sub_type STRING NOT NULL,
    coupled_date DATETIME NOT NULL,
    uncoupled_date DATETIME
) -- CLUSTER BY parent_metering_point_id, metering_point_id;
-- TODO: What to cluster by? --> Bjarke, JMK, Jonas