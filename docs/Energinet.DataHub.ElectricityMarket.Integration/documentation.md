# Electricity Market Integration package

## Registration

Call the following (package included) extension with your `IServiceCollection`.

```c#
services.AddElectricityMarketModule();
```

Make sure that, your configuration includes the following section.

```json
{
    "DatabaseOptions:ConnectionString": "Server=localhost; Datab..."
}
```

## Usage

After above registration, `IElectricityMarketViews` can be resolved. Following views are available.

```c#
Task<MeteringPointMasterData?> GetMeteringPointMasterDataAsync(
        MeteringPointIdentification meteringPointId,
        Instant validAt);

Task<MeteringPointEnergySupplier?> GetMeteringPointEnergySupplierAsync(
    MeteringPointIdentification meteringPointId,
    Instant validAt);
```

All public types can be studied [here](https://github.com/Energinet-DataHub/geh-electricity-market/tree/main/source/electricity-market/ElectricityMarket.Integration).
