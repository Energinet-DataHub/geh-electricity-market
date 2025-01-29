ALTER TABLE [electricitymarket].[EnergySupplyPeriod] ADD
    [WebAccessCode]        varchar(64) NOT NULL,
    [EnergySupplier]       varchar(16) NOT NULL;
GO
FK_EnergySupplyPeriod_EnergySupplyPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[EnergySupplyPeriod]([ID]);