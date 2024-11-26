CREATE VIEW vw_MeteringPointEnergySuppliers AS
SELECT
    mp.Identification,
    cr.EnergySupplier,
    cr.StartDate,
    cr.EndDate
FROM [dbo].[MeteringPoint] mp
JOIN [dbo].[CommercialRelation] cr on mp.Id = cr.MeteringPointId
