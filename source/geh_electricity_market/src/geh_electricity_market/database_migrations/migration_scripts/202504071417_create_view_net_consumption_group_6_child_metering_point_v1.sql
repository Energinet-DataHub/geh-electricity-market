CREATE VIEW {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_child_metering_point_v1 AS 
SELECT
    metering_point_id,
    metering_point_type,
    parent_metering_point_id,
    coupled_date,
    uncoupled_date
FROM {catalog_name}.electricity_market_internal.net_consumption_group_6_child_metering_point