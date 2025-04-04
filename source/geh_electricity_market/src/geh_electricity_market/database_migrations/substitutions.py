from geh_electricity_market.database_migrations.database_definitions import (
    InternalDatabaseDefinition,
    MeasurementsDatabaseDefinition,
)


def substitutions(catalog_name: str) -> dict[str, str]:
    return {
        "{calculated_measurements_database}": MeasurementsDatabaseDefinition.calculated_measurements_database,
        "{internal_database}": InternalDatabaseDefinition.internal_database,
        "{catalog_name}": catalog_name,
    }
