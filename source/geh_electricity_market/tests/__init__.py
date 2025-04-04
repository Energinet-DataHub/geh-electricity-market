from pyspark.sql import SparkSession

from geh_electricity_market.database_migrations.database_definitions import (
    InternalDatabaseDefinition,
    MeasurementsDatabaseDefinition,
)

SPARK_CATALOG_NAME = "spark_catalog"

_CALCULATED_MEASUREMENTS_DATABASE_NAMES = [
    InternalDatabaseDefinition.internal_database,
    MeasurementsDatabaseDefinition.calculated_measurements_database,
]


def ensure_calculated_measurements_databases_exist(spark: SparkSession) -> None:
    """Databases are created in dh3infrastructure using terraform
    So we need to create them in test environment"""
    for db in _CALCULATED_MEASUREMENTS_DATABASE_NAMES:
        spark.sql(f"CREATE DATABASE IF NOT EXISTS {db}")
