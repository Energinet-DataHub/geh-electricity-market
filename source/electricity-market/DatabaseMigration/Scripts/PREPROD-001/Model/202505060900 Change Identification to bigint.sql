ALTER TABLE [electricitymarket].[MeteringPoint]
ALTER COLUMN [Identification] bigint NOT NULL;

ALTER TABLE [electricitymarket].[MeteringPointPeriod]
ALTER COLUMN [ParentIdentification] bigint NULL;

ALTER TABLE [electricitymarket].[QuarantinedMeteringPoint]
ALTER COLUMN [Identification] bigint NOT NULL;
