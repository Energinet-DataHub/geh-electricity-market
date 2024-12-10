CREATE TABLE [dbo].[MeteringPointPeriod]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [MeteringPointId]    bigint NOT NULL,
    [ValidFrom]          datetimeoffset NOT NULL,
    [ValidTo]            datetimeoffset NOT NULL,
    [RetiredById]        bigint NULL,
    [RetiredAt]          datetimeoffset NULL,
    [CreatedAt]          datetimeoffset NOT NULL,
    [GridAreaCode]       char(3) NOT NULL,
    [OwnedBy]            varchar(16) NOT NULL,
    [ConnectionState]    int NOT NULL,
    [Type]               int NOT NULL,
    [SubType]            int NOT NULL,
    [Resolution]         varchar(6) NOT NULL,
    [Unit]               int NOT NULL,
    [ProductId]          int NOT NULL

    CONSTRAINT PK_MeteringPointPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPointPeriod FOREIGN KEY (RetiredById) REFERENCES [dbo].[MeteringPointPeriod]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [dbo].[MeteringPoint]([ID])
)
