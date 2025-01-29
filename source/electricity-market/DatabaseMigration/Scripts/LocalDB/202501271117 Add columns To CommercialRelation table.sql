TRUNCATE TABLE [electricitymarket].[CommercialRelation]

ALTER TABLE [electricitymarket].[CommercialRelation]
    DROP CONSTRAINT FK_CommercialRelation_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID])
GO

ALTER TABLE [electricitymarket].[CommercialRelation] 
    ADD  [CustomerId]         [uniqueidentifier] NOT NULL,
    CONSTRAINT FK_CommercialRelation_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID]);