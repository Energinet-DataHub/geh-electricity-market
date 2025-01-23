CREATE TABLE [electricitymarket].[SpeedTestImportState]
(
    [Id]      int IDENTITY(1,1) NOT NULL,
    [Enabled] bit NOT NULL,
    [Offset]  bigint NOT NULL,

    CONSTRAINT PK_SpeedTestImportState PRIMARY KEY CLUSTERED (Id),
)

CREATE TABLE [electricitymarket].[SpeedTestGold]
(
    [Id]              INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [MeteringPointId] NVARCHAR(18) NOT NULL,
    [ValidFrom]       DATETIMEOFFSET NOT NULL,
    [ValidTo]         DATETIMEOFFSET NOT NULL,
    [CreatedDate]     DATETIMEOFFSET NOT NULL,
    [GridArea]        NVARCHAR(3) NOT NULL,
    [StateId]         BIGINT NOT NULL,
    [TransDossId]     BIGINT NOT NULL
);
GO

CREATE INDEX [IX_SpeedTestGold_TransDossId]
    ON [electricitymarket].[SpeedTestGold] ([TransDossId]);
GO

CREATE INDEX [IX_SpeedTestGold_MeteringPointId]
    ON [electricitymarket].[SpeedTestGold] ([MeteringPointId]);
GO
