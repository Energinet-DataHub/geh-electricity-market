CREATE TABLE [electricitymarket].[QuarantinedMeteringPointTransaction]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [QuarantinedMeteringPointId] bigint NOT NULL,
    [MeteringPointStateId]       bigint NOT NULL,
    [BusinessTransactionDosId]   bigint NOT NULL,
    [Message]                    varchar(1024) NOT NULL,

    CONSTRAINT PK_QuarantinedMeteringPointTransaction PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_QuarantinedMeteringPointTransaction_QuarantinedMeteringPoint FOREIGN KEY (QuarantinedMeteringPointId) REFERENCES [electricitymarket].[QuarantinedMeteringPoint]([Id])
)
