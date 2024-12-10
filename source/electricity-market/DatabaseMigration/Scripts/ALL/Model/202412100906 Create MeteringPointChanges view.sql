CREATE VIEW vw_MeteringPointChanges AS
SELECT
    mp.Identification,
    mpp.ValidFrom,
    mpp.ValidTo,
    mpp.GridAreaCode,
    ga.GridAccessProvider,
    ga.ValidFrom AS GridAccessProviderPeriodFrom,
    ga.ValidTo AS GridAccessProviderPeriodTo,
    mpp.ConnectionState,
    mpp.Type,
    mpp.SubType,
    mpp.Resolution,
    mpp.Unit,
    mpp.ProductId
FROM [dbo].[MeteringPoint] mp
JOIN [dbo].[MeteringPointPeriod] mpp ON mp.Id = mpp.MeteringPointId
JOIN [dbo].[GridArea] ga ON
    mp.GridAreaCode = ga.GridAreaCode AND 
    mp.ValidFrom <= ga.ValidTo AND
    mp.ValidTo >= ga.ValidFrom
WHERE mpp.RetiredById IS NULL