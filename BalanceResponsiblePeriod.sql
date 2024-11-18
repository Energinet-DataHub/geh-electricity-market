CREATE TABLE [dbo].[BalanceResponsiblePeriod] (
  [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
  [CommercialRelationId] BIGINT NOT NULL,
  [ValidFrom] DATETIME,
  [ValidTo] DATETIME,
  [RetiredById] BIGINT,
  [RetiredAt] DATETIME,
  [OrchestrationStepId] BIGINT,
  [DH2BusTransDosiId] BIGINT,
  [BalanceResponsibleGLN] VARCHAR(50),
  CONSTRAINT [FK_BalanceResponsiblePeriod_CommercialRelationId] FOREIGN KEY ([CommercialRelationId]) REFERENCES [dbo].[CommercialRelation]([Id]),
);
