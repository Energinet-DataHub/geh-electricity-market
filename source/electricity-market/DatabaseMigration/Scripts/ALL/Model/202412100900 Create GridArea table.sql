CREATE TABLE [dbo].[GridArea]
(
    [Id]                 int IDENTITY(1,1) NOT NULL,
    [GridAreaCode]       char(3) NOT NULL,
    [GridAccessProvider] varchar(16) NOT NULL,
    [ValidFrom]          datetimeoffset NOT NULL,
    [ValidTo]            datetimeoffset NOT NULL,

    CONSTRAINT PK_GridArea PRIMARY KEY CLUSTERED (Id)
)
