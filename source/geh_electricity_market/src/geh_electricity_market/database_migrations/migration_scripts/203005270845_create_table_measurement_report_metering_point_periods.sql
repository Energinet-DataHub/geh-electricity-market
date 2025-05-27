CREATE TABLE {catalog_name}.electricity_market_internal.measurement_report_metering_point_periods (
    grid_area_code STRING NOT NULL,
    metering_point_id STRING NOT NULL,
    metering_point_type STRING NOT NULL,
    metering_point_resolution STRING NOT NULL,
    energy_supplier_id STRING,
    physical_status BOOL NOT NULL,
    settlement_method STRING,
    unit STRING NOT NULL,
    from_grid_area_code STRING,
    to_grid_area_code STRING,
    period_from_datetime TIMESTAMP NOT NULL,
    period_to_datetime TIMESTAMP
) 
USING DELTA
CLUSTER BY (grid_area_code, metering_point_id, period_from_datetime, period_to_datetime)
TBLPROPERTIES (
    delta.deletedFileRetentionDuration = "interval 30 days"
)