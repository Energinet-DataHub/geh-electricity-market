CREATE TABLE [electricitymarket].[QuarantinedMeteringPoint]
(
    [Id]             bigint IDENTITY(1,1) NOT NULL,
    [Identification] char(18) NOT NULL

    CONSTRAINT PK_QuarantinedMeteringPoint PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_QuarantinedMeteringPoint_Identification UNIQUE (Identification),
)
