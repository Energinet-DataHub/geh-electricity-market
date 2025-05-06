ALTER TABLE [electricitymarket].[MeteringPoint]
DROP CONSTRAINT [UQ_Identification];
GO

ALTER TABLE [electricitymarket].[MeteringPoint]
ALTER COLUMN [Identification] bigint NOT NULL;
GO

ALTER TABLE [electricitymarket].[MeteringPoint]
ADD CONSTRAINT UQ_Identification UNIQUE (Identification);
GO

ALTER TABLE [electricitymarket].[MeteringPointPeriod]
ALTER COLUMN [ParentIdentification] bigint NULL;
GO

ALTER TABLE [electricitymarket].[QuarantinedMeteringPoint]
DROP CONSTRAINT [UQ_QuarantinedMeteringPoint_Identification];
GO

ALTER TABLE [electricitymarket].[QuarantinedMeteringPoint]
ALTER COLUMN [Identification] bigint NOT NULL;
GO

ALTER TABLE [electricitymarket].[QuarantinedMeteringPoint]
ADD CONSTRAINT UQ_QuarantinedMeteringPoint_Identification UNIQUE (Identification);
GO