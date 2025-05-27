CREATE View {catalog_name}.electricity_market_reports_input.measurements_report_metering_point_periods_v1_v1 AS
SELECT
    grid_area_code,
    metering_point_id,
    metering_point_type,
    resolution,
    energy_supplier_id,
    physical_status,
    quantity_unit,
    from_grid_area_code,
    to_grid_area_code,
    period_from_date,
    period_to_date
FROM {catalog_name}.electricity_market_internal.measurements_report_metering_point_periods
