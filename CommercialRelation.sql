CREATE TABLE [dbo].[CommercialRelation] (
  [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
  [MeteringPointId] BIGINT NOT NULL,
  [StartDate] DATETIME,
  [EndDate] DATETIME,
  [EnergySupplierGLN] VARCHAR(50),
  CONSTRAINT [FK_CommercialRelation_MeteringPointId] FOREIGN KEY ([MeteringPointId]) REFERENCES [dbo].[MeteringPoint]([Id])
);
