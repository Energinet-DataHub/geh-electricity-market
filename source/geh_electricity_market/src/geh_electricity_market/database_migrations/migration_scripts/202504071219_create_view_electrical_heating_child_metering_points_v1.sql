CREATE VIEW {catalog_name}.electricity_market_measurements_input.electrical_heating_child_metering_points_v1
AS SELECT (
    metering_point_id,
    metering_point_type,
    metering_point_sub_type,
    parent_metering_point_id,
    coupled_date,
    uncoupled_date
) FROM {catalog_name}.electricity_market_measurements_input.electrical_heating_child_metering_points