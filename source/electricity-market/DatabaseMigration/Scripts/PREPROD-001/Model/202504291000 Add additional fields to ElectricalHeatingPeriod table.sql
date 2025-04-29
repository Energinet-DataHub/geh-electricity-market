ALTER TABLE [electricitymarket].[ElectricalHeatingPeriod]
ADD [MeteringPointStateId] bigint NOT NULL;

ALTER TABLE [electricitymarket].[ElectricalHeatingPeriod]
ADD [BusinessTransactionDosId] bigint NOT NULL;

ALTER TABLE [electricitymarket].[ElectricalHeatingPeriod]
ADD [TransactionType] char(10) NOT NULL;

ALTER TABLE [electricitymarket].[ElectricalHeatingPeriod]
ADD [Active] bit NOT NULL;
