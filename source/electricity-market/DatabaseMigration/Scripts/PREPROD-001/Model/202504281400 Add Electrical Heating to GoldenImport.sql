ALTER TABLE [electricitymarket].[GoldenImport]
ADD [tax_reduction] BIT NULL;

ALTER TABLE [electricitymarket].[GoldenImport]
ADD [tax_settlement_date] DATETIMEOFFSET NULL;
