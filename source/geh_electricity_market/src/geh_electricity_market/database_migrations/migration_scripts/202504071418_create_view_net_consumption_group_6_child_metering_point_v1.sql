CREATE VIEW {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_child_metering_point_v1
AS SELECT (
    metering_point_id COMMENT 'GSRN Number',
    metering_point_type COMMENT 'The metering point type: supply_to_grid, consumption_from_grid, net_consumption',
    parent_metering_point_id COMMENT 'The GSRN number of the parent metering point.',
    coupled_date COMMENT 'UTC time. The date when the child metering point was coupled to the parent metering point.',
    uncoupled_date COMMENT 'UTC time. The date when the child metering point was uncoupled from the parent metering point.'
) FROM {catalog_name}.electricity_market_measurements_input.net_consumption_group_6_child_metering_point