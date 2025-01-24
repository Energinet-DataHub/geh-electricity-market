DROP VIEW IF EXISTS vw_MeteringPointEnergySuppliers;
DROP VIEW IF EXISTS vw_MeteringPointChanges;

DROP TABLE IF EXISTS ElectricalHeatingPeriod;
DROP TABLE IF EXISTS EnergySupplyPeriod;
DROP TABLE IF EXISTS CommercialRelation;
DROP TABLE IF EXISTS MeteringPointPeriod;
DROP TABLE IF EXISTS MeteringPoint;

DROP VIEW IF EXISTS electricitymarket.vw_MeteringPointEnergySuppliers;
DROP VIEW IF EXISTS electricitymarket.vw_MeteringPointChanges;

DROP TABLE IF EXISTS electricitymarket.SpeedTestImportState;
DROP TABLE IF EXISTS electricitymarket.SpeedTestGold;
DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPointTransaction;
DROP TABLE IF EXISTS electricitymarket.QuarantinedMeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ElectricalHeatingPeriod;
DROP TABLE IF EXISTS electricitymarket.EnergySupplyPeriod;
DROP TABLE IF EXISTS electricitymarket.CommercialRelation;
DROP TABLE IF EXISTS electricitymarket.MeteringPointPeriod;
DROP TABLE IF EXISTS electricitymarket.MeteringPoint;
DROP TABLE IF EXISTS electricitymarket.ImportState;

DROP SCHEMA IF EXISTS [electricitymarket];
