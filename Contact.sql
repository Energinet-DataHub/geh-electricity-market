CREATE TABLE [dbo].[Contact] (
  [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
  [EnergySupplierPeriodId] BIGINT,
  [RelationType] VARCHAR(50),
  [Name] VARCHAR(100),
  [Phone] VARCHAR(20),
  [Mobile] VARCHAR(20),
  [Email] VARCHAR(100),
  [Attention] VARCHAR(100),
  [IsProtectedName] BIT,
  [ContactAddressId] BIGINT NOT NULL,
  CONSTRAINT [FK_Contact_EnergySupplierPeriodId] FOREIGN KEY ([EnergySupplierPeriodId]) REFERENCES [dbo].[EnergySupplierPeriod]([Id]),
  CONSTRAINT [FK_Contact_ContactAddressId] FOREIGN KEY ([ContactAddressId]) REFERENCES [dbo].[ContactAddress]([Id])
);
