ALTER TABLE [electricitymarket].[MeteringPoint]
ADD [datetimeoffset] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET());

CREATE INDEX [IX_MeteringPoint_Version]
    ON [electricitymarket].[MeteringPoint] (Version);