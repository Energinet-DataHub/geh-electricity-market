ALTER TABLE [electricitymarket].[GoldenImport]
ADD [location_mp_address_wash_instructions] NVARCHAR(64) NULL;

ALTER TABLE [electricitymarket].[InstallationAddress]
ADD [WashInstructions] nvarchar(64) NULL;