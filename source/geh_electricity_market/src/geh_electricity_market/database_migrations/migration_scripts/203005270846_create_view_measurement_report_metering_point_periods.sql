CREATE View {catalog_name}.electricity_market_reports_input.measurements_report_metering_point_periods_v1_v1 AS
SELECT
    grid_area_code,
    metering_point_id,
    metering_point_type,
    metering_point_resolution,
    energy_supplier_id,
    physical_status,
    settlement_method,
    unit,
    from_grid_area_code,
    to_grid_area_code,
    period_from_datetime,
    period_to_datetime
FROM {catalog_name}.electricity_market_internal.measurements_report_metering_point_periods
