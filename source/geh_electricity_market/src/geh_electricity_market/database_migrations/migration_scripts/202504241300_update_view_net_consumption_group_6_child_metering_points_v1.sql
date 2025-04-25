-- This drop - including "if exists" - is needed as this script is merely an update of the script name timestamp
DROP VIEW IF EXISTS {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_child_metering_points_v1
GO

CREATE VIEW {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_child_metering_points_v1 AS 
SELECT
    metering_point_id,
    metering_point_type,
    parent_metering_point_id,
    coupled_date,
    uncoupled_date
FROM {catalog_name}.electricity_market_internal.net_consumption_group_6_child_metering_point
