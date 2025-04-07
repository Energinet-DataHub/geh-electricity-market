CREATE TABLE {catalog_name}.electricity_market_measurements_input.electrical_heating_child_metering_points (
    metering_point_id STRING NOT NULL COMMENT 'GSRN Number',
    metering_point_type STRING NOT NULL COMMENT 'The type of the metering point. E.g. consumption, production, etc.',
    parent_metering_point_id STRING not NULL COMMENT 'The GSRN number of the parent metering point.',
    metering_point_sub_type STRING NOT NULL COMMENT 'The sub type of the metering point. E.g. consumption, production, etc.',
    coupled_date TIMESTAMP NOT NULL COMMENT 'UTC time. The date when the child metering point was coupled to the parent metering point.',
    uncoupled_date TIMESTAMP COMMENT 'UTC time. The date when the child metering point was uncoupled from the parent metering point.'
) -- CLUSTER BY parent_metering_point_id, metering_point_id;
-- TODO: What to cluster by? --> Bjarke, JMK, Jonas