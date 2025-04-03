# Energinet.DataHub.ElectricityMarket.Integration release notes

## Version 4.3.5

- Added two new Delegation process types

## Version 4.3.4

- Fix incorrect mapping of MeasureUnit.

## Version 4.3.3

- Fix incorrect mapping of ProductId.

## Version 4.3.2

- Internal cleanup of option sectionname, no functional changes.

## Version 4.3.1

- Version Bump, no functional changes

## Version 4.3.0

- Implemented Authorization on client so that i can communicate with the DataAPI as it is now behind authorization

## Version 4.2.3

- fix for health check uri exception

## Version 4.2.2

- Uses TryAddScoped to register service instead of AddScoped
- Renamed options to "ElectricityMarketClientOptions" from "ApiClientOptions"
- Added automatic health checks registrations

## Version 4.2.1

- All methods will now return either null or empty collection if returnings lists, instead of throwing an exception.

## Version 4.2.0

- Removed abstract type ActorNumber from package, replaced with string (Breaking change)
  This means that the receiver is now required to interpret the supplied string to figure out what type it is of GLN/EIC
- Changed date types from

## Version 4.1.2

- internal cleanup

## Version 4.1.1

- updated httpclient urls

## Version 4.1.0

- Removed GetMeteringPointEnergySuppliersAsync (Breaking change)
- Added "EnergySuppliers" property to MasterDataDTO to replace the above removed function
- Added GetProcessDelegationAsync
- Added GetGridAreaOwnerAsync

## Version 4.0.4

- Update packages.

## Version 4.0.3

- Added ValidFrom and ValidTo to MeteringPointMasterData.

## Version 4.0.2

- NeighborGridAreaOwner is now a list NeighborGridAreaOwners.

## Version 4.0.1

- Added NeighborGridAreaOwner as a placeholder field.

## Version 4.0.0

- Rewrite of API to better fit use case.

## Version 3.0.0

- Rewrite of API to better fit use case.

## Version 2.0.0

- Rewrite of API to better fit use case.

## Version 1.0.0

- Initial release
