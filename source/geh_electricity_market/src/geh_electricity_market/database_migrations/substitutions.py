from geh_electricity_market.database_migrations.database_definitions import (
    InternalDatabaseDefinition,
)


def substitutions(catalog_name: str) -> dict[str, str]:
    return {
        "{internal_database}": InternalDatabaseDefinition.INTERNAL_DATABASE,
        "{catalog_name}": catalog_name,
    }
