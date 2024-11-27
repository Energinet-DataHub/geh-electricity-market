# Electricity Market Integration package

## Registration
Invoke the following (package included) extension with your `IServiceCollection`.
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
IAsyncEnumerable<MeteringPointChange> GetMeteringPointChangesAsync(MeteringPointIdentification identification)

IAsyncEnumerable<MeteringPointEnergySupplier> GetMeteringPointEnergySuppliersAsync(MeteringPointIdentification identification)
```

All public types can be studied [here](https://github.com/Energinet-DataHub/geh-electricity-market/tree/main/source/electricity-market/ElectricityMarket.Integration).
