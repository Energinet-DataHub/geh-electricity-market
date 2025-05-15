ALTER TABLE [electricitymarket].[ContactAddress]
ADD [ContactId] bigint NULL;
GO

UPDATE CA
SET    CA.ContactId = C.Id
FROM   electricitymarket.ContactAddress AS CA
JOIN   electricitymarket.Contact        AS C
       ON C.ContactAddressId = CA.Id;

ALTER TABLE [electricitymarket].[ContactAddress]
ALTER COLUMN [ContactId] bigint NOT NULL;
GO

CREATE INDEX [IX_ContactAddress_ContactId]
    ON [electricitymarket].[ContactAddress] (ContactId);
GO

ALTER TABLE [electricitymarket].[Contact]
DROP CONSTRAINT [FK_Contact_ContactAddress];
GO

ALTER TABLE [electricitymarket].[Contact]
DROP COLUMN [ContactAddressId];
