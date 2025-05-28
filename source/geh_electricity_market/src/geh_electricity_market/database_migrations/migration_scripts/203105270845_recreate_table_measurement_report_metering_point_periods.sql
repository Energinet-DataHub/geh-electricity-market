-- Drop the existing table and view to recreate them with the new schema
DROP TABLE {catalog_name}.electricity_market_internal.measurements_report_metering_point_periods
GO

-- Drop the view before recreating the table
DROP VIEW {catalog_name}.electricity_market_reports_input.measurements_report_metering_point_periods_v1
GO


-- Recreate the table with the updated schema
CREATE TABLE {catalog_name}.electricity_market_internal.measurements_report_metering_point_periods (
    grid_area_code STRING NOT NULL,
    metering_point_id STRING NOT NULL,
    metering_point_type STRING NOT NULL,
    resolution STRING NOT NULL,
    energy_supplier_id STRING,
    physical_status STRING NOT NULL,
    settlement_method STRING, -- This is not part of the view, but is included in the table for internal use
    quantity_unit STRING NOT NULL,
    from_grid_area_code STRING,
    to_grid_area_code STRING,
    period_from_date TIMESTAMP NOT NULL,
    period_to_date TIMESTAMP
) 
USING DELTA
CLUSTER BY (grid_area_code, metering_point_id, period_from_date, period_to_date)
TBLPROPERTIES (
    delta.deletedFileRetentionDuration = "interval 30 days"
)
GO

-- Recreate the view to reflect the new table structure
CREATE View {catalog_name}.electricity_market_reports_input.measurements_report_metering_point_periods_v1 AS
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

