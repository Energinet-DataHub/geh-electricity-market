-- CREATE TABLE {catalog_name}.electricity_market_internal.missing_measurements_log_metering_point_periods (
--     metering_point_id STRING NOT NULL COMMENT 'GSRN Number',
--     grid_area_code STRING NOT NULL COMMENT 'The code of the grid area that the metering point belongs to',
--     resolution STRING NOT NULL COMMENT 'Metering point resolution: PT1H/PT15M',
--     period_from_date TIMESTAMP NOT NULL COMMENT 'UTC time. The period start date.',
--     period_to_date TIMESTAMP COMMENT 'UTC time. The period end date.'
-- )
-- USING DELTA

ALTER TABLE {catalog_name}.electricity_market_internal.missing_measurements_log_metering_point_periods
CLUSTER BY (metering_point_id, grid_area_code, period_from_date, period_to_date)
GO

ALTER TABLE {catalog_name}.electricity_market_internal.missing_measurements_log_metering_point_periods 
SET TBLPROPERTIES (
    delta.deletedFileRetentionDuration = "interval 30 days"
)