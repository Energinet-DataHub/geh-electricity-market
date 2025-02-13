DROP VIEW IF EXISTS electricitymarket.vw_MeteringPointEnergySuppliers;
DROP VIEW IF EXISTS electricitymarket.vw_MeteringPointChanges;

DROP TABLE IF EXISTS electricitymarket.SpeedTestGold;
DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPointTransaction;
DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ElectricalHeatingPeriod;
DROP TABLE IF EXISTS electricitymarket.EnergySupplyPeriod;
DROP TABLE IF EXISTS electricitymarket.CommercialRelation;
DROP TABLE IF EXISTS electricitymarket.MeteringPointPeriod;
DROP TABLE IF EXISTS electricitymarket.MeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ImportState;
DROP TABLE IF EXISTS electricitymarket.GoldenImport;

CREATE TABLE [electricitymarket].[MeteringPoint]
(
    [Id]                 bigint NOT NULL,
    [Identification]     char(18) NOT NULL

    CONSTRAINT PK_MeteringPoint PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Identification UNIQUE (Identification),
)

CREATE TABLE [electricitymarket].[MeteringPointPeriod]
(
    [Id]                         bigint NOT NULL,
    [MeteringPointId]            bigint NOT NULL,
    [ValidFrom]                  datetimeoffset NOT NULL,
    [ValidTo]                    datetimeoffset NOT NULL,
    [RetiredById]                bigint NULL,
    [RetiredAt]                  datetimeoffset NULL,
    [CreatedAt]                  datetimeoffset NOT NULL,
    [GridAreaCode]               char(3) NOT NULL,
    [OwnedBy]                    varchar(16) NOT NULL,
    [ConnectionState]            varchar(64) NOT NULL,
    [Type]                       varchar(64) NOT NULL,
    [SubType]                    varchar(64) NOT NULL,
    [Resolution]                 varchar(6) NOT NULL,
    [Unit]                       varchar(64) NOT NULL,
    [ProductId]                  varchar(64) NOT NULL,
    [SettlementGroup]            int NULL,
    [ScheduledMeterReadingMonth] int NOT NULL,
    [ParentIdentification]       char(18) NULL,
    [MeteringPointStateId]       bigint NOT NULL,
    [BusinessTransactionDosId]   bigint NOT NULL,
    [EffectuationDate]           datetimeoffset NOT NULL,
    [TransactionType]            char(10) NOT NULL,

    CONSTRAINT PK_MeteringPointPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPointPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[MeteringPointPeriod]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_ParentIdentification FOREIGN KEY (ParentIdentification) REFERENCES [electricitymarket].[MeteringPoint]([Identification]),
)

CREATE TABLE [electricitymarket].[CommercialRelation]
(
    [Id]                 bigint NOT NULL,
    [MeteringPointId]    bigint NOT NULL,
    [EnergySupplier]     varchar(16) NOT NULL,
    [StartDate]          datetimeoffset NOT NULL,
    [EndDate]            datetimeoffset NOT NULL,
    [ModifiedAt]         datetimeoffset NOT NULL,
    [CustomerId]         uniqueidentifier NULL

    CONSTRAINT PK_CommercialRelation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CommercialRelation_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID])
)

CREATE TABLE [electricitymarket].[ElectricalHeatingPeriod]
(
    [Id]                   bigint NOT NULL,
    [CommercialRelationId] bigint NOT NULL,
    [ValidFrom]            datetimeoffset NOT NULL,
    [ValidTo]              datetimeoffset NOT NULL,
    [RetiredById]          bigint NULL,
    [RetiredAt]            datetimeoffset NULL,
    [CreatedAt]            datetimeoffset NOT NULL

    CONSTRAINT PK_ElectricalHeatingPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ElectricalHeatingPeriod_ElectricalHeatingPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[ElectricalHeatingPeriod]([ID]),
    CONSTRAINT FK_ElectricalHeatingPeriod_CommercialRelation FOREIGN KEY (CommercialRelationId) REFERENCES [electricitymarket].[CommercialRelation]([ID])
)

CREATE TABLE [electricitymarket].[EnergySupplyPeriod]
(
    [Id]                       bigint NOT NULL,
    [CommercialRelationId]     bigint NOT NULL,
    [ValidFrom]                datetimeoffset NOT NULL,
    [ValidTo]                  datetimeoffset NOT NULL,
    [RetiredById]              bigint NULL,
    [RetiredAt]                datetimeoffset NULL,
    [CreatedAt]                datetimeoffset NOT NULL,
    [WebAccessCode]            varchar(64) NOT NULL,
    [EnergySupplier]           varchar(16) NOT NULL,
    [BusinessTransactionDosId] bigint NOT NULL

    CONSTRAINT PK_EnergySupplyPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_EnergySupplyPeriod_EnergySupplyPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[EnergySupplyPeriod]([ID]),
    CONSTRAINT FK_EnergySupplyPeriod_CommercialRelation FOREIGN KEY (CommercialRelationId) REFERENCES [electricitymarket].[CommercialRelation]([ID])
)

CREATE TABLE [electricitymarket].[ImportState]
(
    [Id]      int IDENTITY(1,1) NOT NULL,
    [Enabled] bit NOT NULL,
    [Offset]  bigint NOT NULL,

    CONSTRAINT PK_ImportState PRIMARY KEY CLUSTERED (Id),
)

insert into [electricitymarket].[ImportState](Enabled, Offset)
values (0, 0)
    
GO
CREATE VIEW [electricitymarket].vw_MeteringPointChanges AS
SELECT
    mp.Identification,
    mpp.ValidFrom,
    mpp.ValidTo,
    mpp.GridAreaCode,
    GridAccessProvider = mpp.OwnedBy,
    mpp.ConnectionState,
    mpp.Type,
    mpp.SubType,
    mpp.Resolution,
    mpp.Unit,
    mpp.ProductId,
    mpp.ParentIdentification
FROM [electricitymarket].[MeteringPoint] mp
    JOIN [electricitymarket].[MeteringPointPeriod] mpp ON mp.Id = mpp.MeteringPointId
WHERE mpp.RetiredById IS NULL

GO
CREATE VIEW [electricitymarket].vw_MeteringPointEnergySuppliers AS
SELECT
    mp.Identification,
    cr.EnergySupplier,
    cr.StartDate,
    cr.EndDate
FROM [electricitymarket].[MeteringPoint] mp
    JOIN [electricitymarket].[CommercialRelation] cr ON mp.Id = cr.MeteringPointId
WHERE cr.StartDate < cr.EndDate

GO
CREATE TABLE [electricitymarket].[QuarantinedMeteringPoint]
(
    [Id]             bigint IDENTITY(1,1) NOT NULL,
    [Identification] char(18) NOT NULL

    CONSTRAINT PK_QuarantinedMeteringPoint PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_QuarantinedMeteringPoint_Identification UNIQUE (Identification),
)

CREATE TABLE [electricitymarket].[QuarantinedMeteringPointTransaction]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [QuarantinedMeteringPointId] bigint NOT NULL,
    [MeteringPointStateId]       bigint NOT NULL,
    [BusinessTransactionDosId]   bigint NOT NULL,
    [Message]                    varchar(1024) NOT NULL,

    CONSTRAINT PK_QuarantinedMeteringPointTransaction PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_QuarantinedMeteringPointTransaction_QuarantinedMeteringPoint FOREIGN KEY (QuarantinedMeteringPointId) REFERENCES [electricitymarket].[QuarantinedMeteringPoint]([Id])
)

CREATE TABLE [electricitymarket].[GoldenImport]
(
    [Id]                              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [metering_point_id]               BIGINT         NOT NULL,
    [valid_from_date]                 DATETIMEOFFSET NOT NULL,
    [valid_to_date]                   DATETIMEOFFSET NOT NULL,
    [dh2_created]                     DATETIMEOFFSET NOT NULL,
    [metering_grid_area_id]           CHAR(3)        NOT NULL,
    [metering_point_state_id]         BIGINT         NOT NULL,
    [btd_trans_doss_id]               BIGINT         NOT NULL,
    [physical_status_of_mp]           CHAR(8)        NOT NULL,
    [type_of_mp]                      CHAR(8)        NOT NULL,
    [sub_type_of_mp]                  CHAR(8)        NOT NULL,
    [energy_timeseries_measure_unit]  CHAR(8)        NOT NULL,
    [web_access_code]                 CHAR(10)       NULL,
    [balance_supplier_id]             CHAR(16)       NULL,
    [effectuation_date]               DATETIMEOFFSET NOT NULL,
    [transaction_type]                CHAR(10)       NOT NULL,
)

CREATE INDEX [IX_GoldenImport_metering_point_id]
    ON [electricitymarket].[GoldenImport] (metering_point_id);

CREATE INDEX [IX_GoldenImport_btd_trans_doss_id]
    ON [electricitymarket].[GoldenImport] (btd_trans_doss_id);
