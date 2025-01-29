ALTER TABLE [electricitymarket].[EnergySupplyPeriod]
DROP CONSTRAINT FK_EnergySupplyPeriod_EnergySupplyPeriod 
    DROP CONSTRAINT FK_EnergySupplyPeriod_CommercialRelation
GO

TRUNCATE TABLE [electricitymarket].[EnergySupplyPeriod]
GO


ALTER TABLE [electricitymarket].[EnergySupplyPeriod] ADD
    [WebAccessCode]        varchar(64) NOT NULL,
    [EnergySupplier]       varchar(16) NOT NULL,
    CONSTRAINT FK_EnergySupplyPeriod_EnergySupplyPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[EnergySupplyPeriod]([ID]),
    CONSTRAINT FK_EnergySupplyPeriod_CommercialRelation FOREIGN KEY (CommercialRelationId) REFERENCES [electricitymarket].[CommercialRelation]([ID]);