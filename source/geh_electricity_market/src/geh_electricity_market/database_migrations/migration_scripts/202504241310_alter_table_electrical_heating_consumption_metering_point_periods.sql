-- Enable column mapping mode to enable dropping columns
ALTER TABLE {catalog_name}.electricity_market_internal.electrical_heating_consumption_metering_point_periods SET TBLPROPERTIES ('delta.columnMapping.mode' = 'name')
GO

ALTER TABLE {catalog_name}.electricity_market_internal.electrical_heating_consumption_metering_point_periods
DROP COLUMN has_electrical_heating
