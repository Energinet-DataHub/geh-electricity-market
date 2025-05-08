-- CREATE TABLE {catalog_name}.electricity_market_internal.net_consumption_group_6_child_metering_point (
--     metering_point_id STRING NOT NULL COMMENT 'GSRN Number',
--     metering_point_type STRING NOT NULL COMMENT 'The metering point type: supply_to_grid, consumption_from_grid, net_consumption',
--     parent_metering_point_id STRING not NULL COMMENT 'The GSRN number of the parent metering point.',
--     coupled_date TIMESTAMP NOT NULL COMMENT 'UTC time. The date when the child metering point was coupled to the parent metering point.',
--     uncoupled_date TIMESTAMP COMMENT 'UTC time. The date when the child metering point was uncoupled from the parent metering point.'
-- )
-- USING DELTA

ALTER TABLE {catalog_name}.electricity_market_internal.net_consumption_group_6_child_metering_point
CLUSTER BY (metering_point_id, parent_metering_point_id)
