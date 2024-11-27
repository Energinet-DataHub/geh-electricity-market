IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE name = 'FK_CommercialRelation_MeteringPoint'
)
BEGIN
alter table commercialrelation add constraint FK_CommercialRelation_MeteringPoint foreign key (meteringpointid) references [dbo].[meteringpoint]([id]);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE name = 'FK_MeteringPointPeriod_MeteringPoint'
)
BEGIN
alter table meteringpointperiod add constraint FK_MeteringPointPeriod_MeteringPoint foreign key (meteringpointid) references [dbo].[meteringpoint]([id]);
END