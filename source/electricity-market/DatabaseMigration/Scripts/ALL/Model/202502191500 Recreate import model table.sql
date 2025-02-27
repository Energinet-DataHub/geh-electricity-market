DROP VIEW IF EXISTS electricitymarket.vw_MeteringPointEnergySuppliers;
DROP VIEW IF EXISTS electricitymarket.vw_MeteringPointChanges;

DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPointTransaction;
DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ElectricalHeatingPeriod;
DROP TABLE IF EXISTS electricitymarket.Contact;
DROP TABLE IF EXISTS electricitymarket.ContactAddress;
DROP TABLE IF EXISTS electricitymarket.EnergySupplyPeriod;
DROP TABLE IF EXISTS electricitymarket.CommercialRelation;
DROP TABLE IF EXISTS electricitymarket.MeteringPointPeriod;
DROP TABLE IF EXISTS electricitymarket.InstallationAddress;
DROP TABLE IF EXISTS electricitymarket.MeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ImportState;
DROP TABLE IF EXISTS electricitymarket.GoldenImport;

CREATE TABLE [electricitymarket].[MeteringPoint]
(
    [Id]                 bigint IDENTITY(1,1) NOT NULL,
    [Identification]     char(18) NOT NULL

    CONSTRAINT PK_MeteringPoint PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Identification UNIQUE (Identification),
)

CREATE TABLE [electricitymarket].[InstallationAddress]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [StreetCode]                 char(4) NULL,
    [StreetName]                 nvarchar(64) NOT NULL,
    [BuildingNumber]             nvarchar(64) NOT NULL,
    [CityName]                   nvarchar(64) NOT NULL,
    [CitySubdivisionName]        nvarchar(64) NULL,
    [DarReference]               uniqueidentifier NULL,
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
    [ParentIdentification]       char(18) NULL,
    
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
    [PowerLimitKw]               int NULL,
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
    [EffectuationDate]           datetimeoffset NOT NULL,
    [TransactionType]            char(10) NOT NULL,

    CONSTRAINT PK_MeteringPointPeriod PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPointPeriod FOREIGN KEY (RetiredById) REFERENCES [electricitymarket].[MeteringPointPeriod]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_MeteringPoint FOREIGN KEY (MeteringPointId) REFERENCES [electricitymarket].[MeteringPoint]([ID]),
    CONSTRAINT FK_MeteringPointPeriod_ParentIdentification FOREIGN KEY (ParentIdentification) REFERENCES [electricitymarket].[MeteringPoint]([Identification]),
    CONSTRAINT FK_MeteringPointPeriod_InstallationAddress FOREIGN KEY (InstallationAddressId) REFERENCES [electricitymarket].[InstallationAddress]([ID]),
)

CREATE INDEX [IX_MeteringPointPeriod_MeteringPointId]
    ON [electricitymarket].[MeteringPointPeriod] (MeteringPointId);

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
    [Id]                   bigint IDENTITY(1,1) NOT NULL,
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
    [Id]                       bigint IDENTITY(1,1) NOT NULL,
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

CREATE INDEX [IX_EnergySupplyPeriod_CommercialRelationId]
    ON [electricitymarket].[EnergySupplyPeriod] (CommercialRelationId);

CREATE TABLE [electricitymarket].[ContactAddress]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [IsProtectedAddress]         bit NOT NULL,
    [Attention]                  nvarchar(128) NOT NULL,
    [StreetCode]                 char(4) NULL,
    [StreetName]                 nvarchar(64) NOT NULL,
    [BuildingNumber]             nvarchar(64) NOT NULL,
    [CityName]                   nvarchar(64) NOT NULL,
    [CitySubdivisionName]        nvarchar(64) NULL,
    [DarReference]               uniqueidentifier NULL,
    [CountryCode]                nvarchar(64) NOT NULL,
    [Floor]                      nvarchar(64) NULL,
    [Room]                       nvarchar(64) NULL,
    [PostCode]                   nvarchar(64) NOT NULL,
    [MunicipalityCode]           nvarchar(64) NULL,

    CONSTRAINT PK_ContactAddress PRIMARY KEY CLUSTERED (Id),
)

CREATE TABLE [electricitymarket].[Contact]
(
    [Id]                         bigint IDENTITY(1,1) NOT NULL,
    [EnergySupplyPeriodId]       bigint NOT NULL,
    
    [RelationType]               varchar(64) NOT NULL,
    [DisponentName]              nvarchar(128) NOT NULL,
    [CPR]                        char(12) NULL,
    [CVR]                        char(8) NULL,

    [ContactAddressId]           bigint NULL,
    [ContactName]                nvarchar(128) NULL,
    [Email]                      nvarchar(128) NULL,
    [Phone]                      nvarchar(64) NULL,
    [Mobile]                     nvarchar(64) NULL,
    [IsProtectedName]            bit NOT NULL,

    CONSTRAINT PK_Contact PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Contact_EnergySupplyPeriod FOREIGN KEY (EnergySupplyPeriodId) REFERENCES [electricitymarket].[EnergySupplyPeriod]([ID]),
    CONSTRAINT FK_Contact_ContactAddress FOREIGN KEY (ContactAddressId) REFERENCES [electricitymarket].[ContactAddress]([ID]),
)

CREATE INDEX [IX_Contact_EnergySupplyPeriod]
    ON [electricitymarket].[Contact] (EnergySupplyPeriodId);

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
CREATE TABLE [electricitymarket].[QuarantinedMeteringPoint]
(
    [Id]             bigint IDENTITY(1,1) NOT NULL,
    [Identification] char(18) NOT NULL,
    [Message]        varchar(max) NOT NULL

    CONSTRAINT PK_QuarantinedMeteringPoint PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_QuarantinedMeteringPoint_Identification UNIQUE (Identification),
)

CREATE TABLE [electricitymarket].[GoldenImport]
(
    [Id]                              INT                NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [metering_point_id]               BIGINT             NOT NULL,
    [valid_from_date]                 DATETIMEOFFSET     NOT NULL,
    [valid_to_date]                   DATETIMEOFFSET     NOT NULL,
    [dh2_created]                     DATETIMEOFFSET     NOT NULL,
    [metering_grid_area_id]           CHAR(3)            NOT NULL,
    [metering_point_state_id]         BIGINT             NOT NULL,
    [btd_trans_doss_id]               BIGINT             NOT NULL,
    [parent_metering_point_id]        CHAR(18)           NULL,
    [type_of_mp]                      CHAR(3)            NOT NULL,
    [sub_type_of_mp]                  CHAR(3)            NOT NULL,
    [physical_status_of_mp]           CHAR(3)            NOT NULL,
    [web_access_code]                 CHAR(10)           NULL,
    [balance_supplier_id]             CHAR(16)           NULL,
    [effectuation_date]               DATETIMEOFFSET     NOT NULL,
    [transaction_type]                CHAR(10)           NOT NULL,
    [meter_reading_occurrence]        CHAR(8)            NOT NULL,
    [mp_connection_type]              CHAR(3)            NULL,
    [disconnection_type]              CHAR(3)            NULL,
    [product]                         CHAR(13)           NOT NULL,
    [product_obligation]              BIT                NULL,
    [energy_timeseries_measure_unit]  CHAR(8)            NOT NULL,
    [asset_type]                      CHAR(3)            NULL,
    [fuel_type]                       BIT                NULL,
    [mp_capacity]                     CHAR(20)           NULL,
    [power_limit_kw]                  INT                NULL,
    [power_limit_a]                   INT                NULL,
    [meter_number]                    CHAR(20)           NULL,
    [net_settlement_group]            INT                NULL,
    [scheduled_meter_reading_date01]  CHAR(4)            NULL,
    [from_grid_area]                  CHAR(3)            NULL,
    [to_grid_area]                    CHAR(3)            NULL,
    [power_plant_gsrn]                CHAR(18)           NULL,
    [settlement_method]               CHAR(3)            NULL,

    [location_street_code]            CHAR(4)            NULL,
    [location_street_name]            NVARCHAR(64)       NULL,
    [location_building_number]        NVARCHAR(64)       NULL,
    [location_city_name]              NVARCHAR(64)       NULL,
    [location_city_subdivision_name]  NVARCHAR(64)       NULL,
    [location_dar_reference]          NVARCHAR(36)       NULL,
    [location_country_name]           NVARCHAR(64)       NULL,
    [location_floor_id]               NVARCHAR(64)       NULL,
    [location_room_id]                NVARCHAR(64)       NULL,
    [location_postcode]               NVARCHAR(64)       NULL,
    [location_municipality_code]      NVARCHAR(64)       NULL,
    [location_location_description]   NVARCHAR(512)      NULL,

    [first_consumer_party_name]       NVARCHAR(128)      NULL,
    [first_consumer_cpr]              CHAR(10)           NULL,
    [second_consumer_party_name]      NVARCHAR(128)      NULL,
    [second_consumer_cpr]             CHAR(10)           NULL,
    [consumer_cvr]                    CHAR(8)            NULL,
    [protected_name]                  BIT                NULL,

    [contact_1_contact_name1]         NVARCHAR(128)      NULL,
    [contact_1_protected_address]     BIT                NULL,
    [contact_1_phone_number]          NVARCHAR(64)       NULL,
    [contact_1_mobile_number]         NVARCHAR(64)       NULL,
    [contact_1_email_address]         NVARCHAR(128)      NULL,
    [contact_1_attention]             NVARCHAR(64)       NULL,
    [contact_1_street_code]           CHAR(4)            NULL,
    [contact_1_street_name]           NVARCHAR(64)       NULL,
    [contact_1_building_number]       NVARCHAR(64)       NULL,
    [contact_1_postcode]              NVARCHAR(64)       NULL,
    [contact_1_city_name]             NVARCHAR(64)       NULL,
    [contact_1_city_subdivision_name] NVARCHAR(64)       NULL,
    [contact_1_dar_reference]         NVARCHAR(36)       NULL,
    [contact_1_country_name]          NVARCHAR(64)       NULL,
    [contact_1_floor_id]              NVARCHAR(64)       NULL,
    [contact_1_room_id]               NVARCHAR(64)       NULL,
    [contact_1_post_box]              NVARCHAR(64)       NULL,
    [contact_1_municipality_code]     NVARCHAR(64)       NULL,

    [contact_4_contact_name1]         NVARCHAR(128)      NULL,
    [contact_4_protected_address]     BIT                NULL,
    [contact_4_phone_number]          NVARCHAR(64)       NULL,
    [contact_4_mobile_number]         NVARCHAR(64)       NULL,
    [contact_4_email_address]         NVARCHAR(128)      NULL,
    [contact_4_attention]             NVARCHAR(64)       NULL,
    [contact_4_street_code]           CHAR(4)            NULL,
    [contact_4_street_name]           NVARCHAR(64)       NULL,
    [contact_4_building_number]       NVARCHAR(64)       NULL,
    [contact_4_postcode]              NVARCHAR(64)       NULL,
    [contact_4_city_name]             NVARCHAR(64)       NULL,
    [contact_4_city_subdivision_name] NVARCHAR(64)       NULL,
    [contact_4_dar_reference]         NVARCHAR(36)       NULL,
    [contact_4_country_name]          NVARCHAR(64)       NULL,
    [contact_4_floor_id]              NVARCHAR(64)       NULL,
    [contact_4_room_id]               NVARCHAR(64)       NULL,
    [contact_4_post_box]              NVARCHAR(64)       NULL,
    [contact_4_municipality_code]     NVARCHAR(64)       NULL,
)

CREATE INDEX [IX_GoldenImport_metering_point_id]
    ON [electricitymarket].[GoldenImport] (metering_point_id);

CREATE INDEX [IX_GoldenImport_btd_trans_doss_id]
    ON [electricitymarket].[GoldenImport] (btd_trans_doss_id);
