TRUNCATE TABLE [electricitymarket].[EnergySupplyPeriod]

ALTER TABLE [electricitymarket].[CommercialRelation] ADD
    [WebAccessCode]        varchar(64) NOT NULL,
    [EnergySupplier]       varchar(16) NOT NULL;