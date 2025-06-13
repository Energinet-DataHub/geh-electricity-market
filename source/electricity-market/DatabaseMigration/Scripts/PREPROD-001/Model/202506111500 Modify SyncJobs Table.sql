ALTER TABLE [electricitymarket].[SyncJobs]
ADD [MeteringPointId] BIGINT NULL
GO

UPDATE [electricitymarket].[SyncJobs] 
SET [MeteringPointId] = 0
GO

ALTER TABLE [electricitymarket].[SyncJobs]
ALTER COLUMN [MeteringPointId] BIGINT NOT NULL;