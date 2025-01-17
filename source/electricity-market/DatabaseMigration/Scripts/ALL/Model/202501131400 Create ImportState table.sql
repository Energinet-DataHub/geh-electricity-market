CREATE TABLE [electricitymarket].[ImportState]
(
    [Id]      int IDENTITY(1,1) NOT NULL,
    [Enabled] bit NOT NULL,
    [Offset]  bigint NOT NULL,

    CONSTRAINT PK_ImportState PRIMARY KEY CLUSTERED (Id),
)
