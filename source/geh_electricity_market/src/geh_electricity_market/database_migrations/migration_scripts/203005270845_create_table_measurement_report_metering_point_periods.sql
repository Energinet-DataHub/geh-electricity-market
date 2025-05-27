CREATE TABLE {catalog_name}.electricity_market_internal.measurements_report_metering_point_periods (
    grid_area_code STRING NOT NULL,
    metering_point_id STRING NOT NULL,
    metering_point_type STRING NOT NULL,
    resolution STRING NOT NULL,
    energy_supplier_id STRING,
    physical_status BOOLEAN NOT NULL,
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