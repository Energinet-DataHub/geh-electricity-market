CREATE VIEW vw_MeteringPointChanges AS
SELECT
    mp.Identification,
    mpp.ValidFrom,
    mpp.ValidTo,
    mpp.GridAreaCode,
    mpp.GridAccessProvider,
    mpp.ConnectionState,
    mpp.SubType,
    mpp.Resolution,
    mpp.Unit,
    mpp.ProductId
FROM [dbo].[MeteringPoint] mp
JOIN [dbo].[MeteringPointPeriod] mpp on mp.Id = mpp.MeteringPointId
