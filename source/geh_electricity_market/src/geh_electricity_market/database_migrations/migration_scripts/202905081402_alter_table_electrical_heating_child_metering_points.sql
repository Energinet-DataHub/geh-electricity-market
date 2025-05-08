-- CREATE TABLE {catalog_name}.electricity_market_internal.electrical_heating_child_metering_points (
--     metering_point_id STRING NOT NULL COMMENT 'GSRN Number',
--     metering_point_type STRING NOT NULL COMMENT 'The type of the metering point. E.g. consumption, production, etc.',
--     parent_metering_point_id STRING not NULL COMMENT 'The GSRN number of the parent metering point.',
--     metering_point_sub_type STRING NOT NULL COMMENT 'The sub type of the metering point. E.g. consumption, production, etc.',
--     coupled_date TIMESTAMP NOT NULL COMMENT 'UTC time. The date when the child metering point was coupled to the parent metering point.',
--     uncoupled_date TIMESTAMP COMMENT 'UTC time. The date when the child metering point was uncoupled from the parent metering point.'
-- )
-- USING DELTA

ALTER TABLE {catalog_name}.electricity_market_internal.electrical_heating_child_metering_points
CLUSTER BY (metering_point_id, parent_metering_point_id)
GO

ALTER TABLE {catalog_name}.electricity_market_internal.electrical_heating_child_metering_points 
SET TBLPROPERTIES (
    delta.deletedFileRetentionDuration = "interval 30 days"
)