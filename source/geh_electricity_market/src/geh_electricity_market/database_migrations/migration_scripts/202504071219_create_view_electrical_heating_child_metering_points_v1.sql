CREATE VIEW {catalog_name}.electricity_market_measurements_input.electrical_heating_child_metering_points_v1
AS SELECT (
    metering_point_id COMMENT 'GSRN Number',
    metering_point_type COMMENT 'The type of the metering point. E.g. consumption, production, etc.',
    parent_metering_point_id COMMENT 'The GSRN number of the parent metering point.',
    metering_point_sub_type COMMENT 'The sub type of the metering point. E.g. consumption, production, etc.',
    coupled_date COMMENT 'UTC time. The date when the child metering point was coupled to the parent metering point.',
    uncoupled_date COMMENT 'UTC time. The date when the child metering point was uncoupled from the parent metering point.'
) FROM {catalog_name}.electricity_market_measurements_input.electrical_heating_child_metering_points