CREATE TABLE [dbo].[Customer] (
  [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
  [ContactId] BIGINT,
  [Name] VARCHAR(100),
  [CPR] VARCHAR(50),
  [CVR] VARCHAR(50),
  CONSTRAINT [FK_Customer_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [dbo].[Contact]([Id])
);
