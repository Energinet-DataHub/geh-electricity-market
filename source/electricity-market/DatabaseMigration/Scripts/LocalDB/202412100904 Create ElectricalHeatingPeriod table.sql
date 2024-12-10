CREATE TABLE [dbo].[ElectricalHeatingPeriod]
(
    [Id]                   bigint IDENTITY(1,1) NOT NULL,
    [CommercialRelationId] bigint NOT NULL,
    [ValidFrom]            datetimeoffset NOT NULL,
    [ValidTo]              datetimeoffset NOT NULL,
    [RetiredById]          bigint NULL,
    [RetiredAt]            datetimeoffset NULL,
    [CreatedAt]            datetimeoffset NOT NULL

    CONSTRAINT PK_ElectricalHeatingPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ElectricalHeatingPeriod_ElectricalHeatingPeriod FOREIGN KEY (RetiredById) REFERENCES [dbo].[ElectricalHeatingPeriod]([ID]),
    CONSTRAINT FK_ElectricalHeatingPeriod_CommercialRelation FOREIGN KEY (CommercialRelationId) REFERENCES [dbo].[CommercialRelation]([ID])
)
