ALTER TABLE [electricitymarket].[GoldenImport]
ALTER COLUMN [power_limit_kw] DECIMAL(11,1) NULL;

ALTER TABLE [electricitymarket].[MeteringPointPeriod]
ALTER COLUMN [PowerLimitKw] DECIMAL(11,1) NULL;

ALTER TABLE [electricitymarket].[MeteringPointPeriod]
DROP COLUMN [EffectuationDate];
