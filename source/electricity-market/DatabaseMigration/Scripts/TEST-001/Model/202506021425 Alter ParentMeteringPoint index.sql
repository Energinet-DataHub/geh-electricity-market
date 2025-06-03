CREATE NONCLUSTERED INDEX [IX_MeteringPointPeriod_ParentIdentification] ON [electricitymarket].[MeteringPointPeriod]
(
	[ParentIdentification] ASC
)
INCLUDE([MeteringPointId]) WITH (DROP_EXISTING = ON)
