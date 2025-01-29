ALTER TABLE [electricitymarket].[CommercialRelation]
    DROP CONSTRAINT FK_CommercialRelation_MeteringPoint
GO

TRUNCATE TABLE [electricitymarket].[CommercialRelation]
    GO

ALTER TABLE [electricitymarket].[CommercialRelation] 
    ADD  [CustomerId]         [uniqueidentifier] NOT NULL;

ALTER TABLE [electricitymarket].[CommercialRelation] 
    ADD CONSTRAINT FK_CommercialRelation_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID]);