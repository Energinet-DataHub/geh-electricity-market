ALTER TABLE [electricitymarket].[MeteringPoint]
ADD [Version] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET());

CREATE INDEX [IX_MeteringPoint_Version]
    ON [electricitymarket].[MeteringPoint] (Version);