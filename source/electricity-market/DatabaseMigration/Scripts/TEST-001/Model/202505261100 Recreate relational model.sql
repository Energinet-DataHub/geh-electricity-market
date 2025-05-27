DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPointTransaction;
DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ElectricalHeatingPeriod;
DROP TABLE IF EXISTS electricitymarket.ContactAddress;
DROP TABLE IF EXISTS electricitymarket.Contact;
DROP TABLE IF EXISTS electricitymarket.EnergySupplyPeriod;
DROP TABLE IF EXISTS electricitymarket.CommercialRelation;
DROP TABLE IF EXISTS electricitymarket.MeteringPointPeriod;
DROP TABLE IF EXISTS electricitymarket.InstallationAddress;
DROP TABLE IF EXISTS electricitymarket.MeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ImportState;

CREATE TABLE [electricitymarket].[MeteringPoint]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [Identification]     bigint               NOT NULL,
    [Version]            datetimeoffset       NOT NULL

    CONSTRAINT PK_MeteringPoint PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Identification UNIQUE (Identification),
)

CREATE INDEX [IX_MeteringPoint_Version]
    ON [electricitymarket].[MeteringPoint] (Version)

CREATE TABLE [electricitymarket].[InstallationAddress]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [StreetCode]                 char(4) NULL,
    [StreetName]                 nvarchar(64) NOT NULL,
    [BuildingNumber]             nvarchar(64) NOT NULL,
    [CityName]                   nvarchar(64) NOT NULL,
    [CitySubdivisionName]        nvarchar(64) NULL,
    [DarReference]               uniqueidentifier NULL,
    [WashInstructions]           nvarchar(64) NULL,
    [CountryCode]                nvarchar(64) NOT NULL,
    [Floor]                      nvarchar(64) NULL,
    [Room]                       nvarchar(64) NULL,
    [PostCode]                   nvarchar(64) NOT NULL,
    [MunicipalityCode]           nvarchar(64) NULL,
    [LocationDescription]        nvarchar(512) NULL,

    CONSTRAINT PK_InstallationAddress PRIMARY KEY CLUSTERED (Id),
)

CREATE TABLE [electricitymarket].[MeteringPointPeriod]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [MeteringPointId]            bigint NOT NULL,
    [ValidFrom]                  datetimeoffset NOT NULL,
    [ValidTo]                    datetimeoffset NOT NULL,
    [RetiredById]                bigint NULL,
    [RetiredAt]                  datetimeoffset NULL,
    [CreatedAt]                  datetimeoffset NOT NULL,
    [ParentIdentification]       bigint NULL,
    
    [Type]                       varchar(64) NOT NULL,
    [SubType]                    varchar(64) NOT NULL,
    [ConnectionState]            varchar(64) NOT NULL,
    [Resolution]                 varchar(6) NOT NULL,
    [GridAreaCode]               char(3) NOT NULL,
    [OwnedBy]                    varchar(16) NULL,
    [ConnectionType]             varchar(64) NULL,
    [DisconnectionType]          varchar(64) NULL,
    [Product]                    varchar(64) NOT NULL,
    [ProductObligation]          bit NULL,
    [MeasureUnit]                varchar(64) NOT NULL,
    [AssetType]                  varchar(64) NULL,
    [FuelType]                   bit NULL,
    [Capacity]                   varchar(20) NULL,
    [PowerLimitKw]               decimal(11, 1) NULL,
    [PowerLimitA]                int NULL,
    [MeterNumber]                varchar(20) NULL,
    [SettlementGroup]            int NULL,
    [ScheduledMeterReadingMonth] int NULL,
    [ExchangeFromGridArea]       char(3) NULL,
    [ExchangeToGridArea]         char(3) NULL,
    [PowerPlantGsrn]             char(18) NULL,
    [SettlementMethod]           varchar(64) NULL,
    [InstallationAddressId]      bigint NOT NULL,

    [MeteringPointStateId]       bigint NOT NULL,
    [BusinessTransactionDosId]   bigint NOT NULL,
    [TransactionType]            char(10) NOT NULL,

    CONSTRAINT PK_MeteringPointPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPointPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[MeteringPointPeriod]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_InstallationAddress FOREIGN KEY (InstallationAddressId) REFERENCES [electricitymarket].[InstallationAddress]([ID])
)

CREATE INDEX [IX_MeteringPointPeriod_MeteringPointId]
    ON [electricitymarket].[MeteringPointPeriod] (MeteringPointId);

CREATE INDEX [IX_MeteringPointPeriod_GridAreaCode]
    ON [electricitymarket].[MeteringPointPeriod] (GridAreaCode);

CREATE INDEX [IX_MeteringPointPeriod_ParentIdentification]
    ON [electricitymarket].[MeteringPointPeriod] (ParentIdentification);

CREATE TABLE [electricitymarket].[CommercialRelation]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [MeteringPointId]    bigint NOT NULL,
    [EnergySupplier]     varchar(16) NOT NULL,
    [StartDate]          datetimeoffset NOT NULL,
    [EndDate]            datetimeoffset NOT NULL,
    [ModifiedAt]         datetimeoffset NOT NULL,
    [ClientId]           uniqueidentifier NULL

    CONSTRAINT PK_CommercialRelation PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CommercialRelation_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID])
)

CREATE INDEX [IX_CommercialRelation_MeteringPointId]
    ON [electricitymarket].[CommercialRelation] (MeteringPointId);

CREATE TABLE [electricitymarket].[ElectricalHeatingPeriod]
(
    [Id]                       bigint IDENTITY(1,1) NOT NULL,
    [CommercialRelationId]     bigint NOT NULL,
    [ValidFrom]                datetimeoffset NOT NULL,
    [ValidTo]                  datetimeoffset NOT NULL,
    [RetiredById]              bigint NULL,
    [RetiredAt]                datetimeoffset NULL,
    [CreatedAt]                datetimeoffset NOT NULL,
    [MeteringPointStateId]     bigint NOT NULL,
    [BusinessTransactionDosId] bigint NOT NULL,
    [TransactionType]          char(10) NOT NULL,
    [Active]                   bit NOT NULL,

    CONSTRAINT PK_ElectricalHeatingPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ElectricalHeatingPeriod_ElectricalHeatingPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[ElectricalHeatingPeriod]([ID]),
    CONSTRAINT FK_ElectricalHeatingPeriod_CommercialRelation FOREIGN KEY (CommercialRelationId) REFERENCES [electricitymarket].[CommercialRelation]([ID])
)

CREATE INDEX [IX_ElectricalHeatingPeriod_CommercialRelationId]
    ON [electricitymarket].[ElectricalHeatingPeriod] (CommercialRelationId);


CREATE TABLE [electricitymarket].[EnergySupplyPeriod]
(
    [Id]                       bigint IDENTITY(1,1) NOT NULL,
    [CommercialRelationId]     bigint NOT NULL,
    [ValidFrom]                datetimeoffset NOT NULL,
    [ValidTo]                  datetimeoffset NOT NULL,
    [RetiredById]              bigint NULL,
    [RetiredAt]                datetimeoffset NULL,
    [CreatedAt]                datetimeoffset NOT NULL,
    [WebAccessCode]            varchar(64) NOT NULL,
    [EnergySupplier]           varchar(16) NOT NULL,
    [BusinessTransactionDosId] bigint NOT NULL,
    [TransactionType]          char(10) NOT NULL,

    CONSTRAINT PK_EnergySupplyPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_EnergySupplyPeriod_EnergySupplyPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[EnergySupplyPeriod]([ID]),
    CONSTRAINT FK_EnergySupplyPeriod_CommercialRelation FOREIGN KEY (CommercialRelationId) REFERENCES [electricitymarket].[CommercialRelation]([ID])
)

CREATE INDEX [IX_EnergySupplyPeriod_CommercialRelationId]
    ON [electricitymarket].[EnergySupplyPeriod] (CommercialRelationId);

CREATE TABLE [electricitymarket].[Contact]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [EnergySupplyPeriodId]       bigint NOT NULL,
    
    [RelationType]               varchar(64) NOT NULL,
    [DisponentName]              nvarchar(256) NOT NULL,
    [CPR]                        char(10) NULL,
    [CVR]                        char(8) NULL,

    [ContactName]                nvarchar(256) NULL,
    [Email]                      nvarchar(256) NULL,
    [Phone]                      nvarchar(64) NULL,
    [Mobile]                     nvarchar(64) NULL,
    [IsProtectedName]            bit NOT NULL,

    CONSTRAINT PK_Contact PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Contact_EnergySupplyPeriod FOREIGN KEY (EnergySupplyPeriodId) REFERENCES [electricitymarket].[EnergySupplyPeriod]([ID])
)

CREATE INDEX [IX_Contact_EnergySupplyPeriod]
    ON [electricitymarket].[Contact] (EnergySupplyPeriodId);

CREATE TABLE [electricitymarket].[ContactAddress]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [ContactId]                  bigint NOT NULL,
    [IsProtectedAddress]         bit NOT NULL,
    [Attention]                  nvarchar(256) NULL,
    [StreetCode]                 char(4) NULL,
    [StreetName]                 nvarchar(64) NULL,
    [BuildingNumber]             nvarchar(64) NULL,
    [CityName]                   nvarchar(64) NULL,
    [CitySubdivisionName]        nvarchar(64) NULL,
    [DarReference]               uniqueidentifier NULL,
    [CountryCode]                nvarchar(64) NULL,
    [Floor]                      nvarchar(64) NULL,
    [Room]                       nvarchar(64) NULL,
    [PostCode]                   nvarchar(64) NULL,
    [PostBox]                    nvarchar(64) NULL,
    [MunicipalityCode]           nvarchar(64) NULL,

    CONSTRAINT PK_ContactAddress PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ContactAddress_Contact FOREIGN KEY (ContactId) REFERENCES [electricitymarket].[Contact]([ID])
)

CREATE INDEX [IX_ContactAddress_ContactId]
    ON [electricitymarket].[ContactAddress] (ContactId);

CREATE TABLE [electricitymarket].[ImportState]
(
    [Id]      int IDENTITY(1,1) NOT NULL,
    [Mode]    int NOT NULL,
    [Offset]  bigint NOT NULL,

    CONSTRAINT PK_ImportState PRIMARY KEY CLUSTERED (Id),
)
GO

INSERT INTO [electricitymarket].[ImportState]([Mode], [Offset])
VALUES (0, 0)

GO
CREATE TABLE [electricitymarket].[QuarantinedMeteringPoint]
(
    [Id]             bigint IDENTITY(1,1) NOT NULL,
    [Identification] bigint NOT NULL,
    [Message]        varchar(max) NOT NULL

    CONSTRAINT PK_QuarantinedMeteringPoint PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_QuarantinedMeteringPoint_Identification UNIQUE (Identification),
)
