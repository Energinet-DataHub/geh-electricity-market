CREATE TABLE [electricitymarket].[GoldenImport]
(
    [Id]                              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [metering_point_id]               BIGINT         NOT NULL,
    [valid_from_date]                 DATETIMEOFFSET NOT NULL,
    [valid_to_date]                   DATETIMEOFFSET NOT NULL,
    [dh2_created]                     DATETIMEOFFSET NOT NULL,
    [metering_grid_area_id]           VARCHAR(3)     NOT NULL,
    [metering_point_state_id]         BIGINT         NOT NULL,
    [btd_business_trans_doss_id]      BIGINT         NOT NULL,
    [physical_status_of_mp]           VARCHAR(64)    NOT NULL,
    [type_of_mp]                      VARCHAR(64)    NOT NULL,
    [sub_type_of_mp]                  VARCHAR(64)    NOT NULL,
    [energy_timeseries_measure_unit]  VARCHAR(64)    NOT NULL
);

CREATE INDEX [IX_GoldenImport_metering_point_id]
    ON [electricitymarket].[GoldenImport] (metering_point_id);

CREATE INDEX [IX_GoldenImport_btd_business_trans_doss_id]
    ON [electricitymarket].[GoldenImport] (btd_business_trans_doss_id);
