from geh_electricity_market.database_migrations.database_definitions import InternalDatabaseDefinition

SPARK_CATALOG_NAME = "spark_catalog"

DATAPRODUCTS_DATABASE_NAME = "electricity_market_measurements_input"

REQUIRED_DATABASES = [
    InternalDatabaseDefinition.INTERNAL_DATABASE,
    DATAPRODUCTS_DATABASE_NAME,
    "electricity_market_reports_input",
]
