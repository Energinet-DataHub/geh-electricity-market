CREATE TABLE [dbo].[MeteringPointPeriod]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [MeteringPointId]    bigint NOT NULL,
    [ValidFrom]          datetimeoffset NOT NULL,
    [ValidTo]            datetimeoffset NOT NULL,
    [GridAreaCode]       char(3) NOT NULL,
    [GridAccessProvider] varchar(16) NOT NULL,
    [ConnectionState]    int NOT NULL,
    [SubType]            int NOT NULL,
    [Resolution]         varchar(6) NOT NULL,
    [Unit]               int NOT NULL,
    [ProductId]          int NOT NULL

    CONSTRAINT PK_MeteringPointPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [dbo].[MeteringPoint]([ID])
)
