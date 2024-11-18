CREATE TABLE [dbo].[EnergySupplierPeriod] (
  [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
  [CommercialRelationId] BIGINT NOT NULL,
  [ValidFrom] DATETIME,
  [ValidTo] DATETIME,
  [RetiredById] BIGINT,
  [RetiredAt] DATETIME,
  [OrchestrationStepId] BIGINT,
  [DH2BusTransDosiId] BIGINT,
  CONSTRAINT [FK_EnergySupplierPeriod_CommercialRelationId] FOREIGN KEY ([CommercialRelationId]) REFERENCES [dbo].[CommercialRelation]([Id])
);
