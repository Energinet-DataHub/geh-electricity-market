### This file contains the fixtures that are used in the tests. ###
import shutil
import sys
from typing import Generator
from unittest import mock

import pytest
from geh_common.testing.spark.spark_test_session import get_spark_test_session
from pyspark.sql import SparkSession

from geh_electricity_market.database_migrations.migrations_runner import _migrate
from tests import DATABASE_NAMES, SPARK_CATALOG_NAME

# pytest-xdist plugin does not work with SparkSession as a fixture. The session scope is not supported.
# Therefore, we need to create a global variable to store the Spark session and data directory.
# This is a workaround to avoid creating a new Spark session for each test.
_spark, data_dir = get_spark_test_session()


@pytest.fixture(scope="session")
def spark() -> Generator[SparkSession, None, None]:
    """
    Create a Spark session with Delta Lake enabled.
    """
    yield _spark
    _spark.stop()
    shutil.rmtree(data_dir)


@pytest.fixture(scope="session", autouse=True)
def fix_print():
    """
    pytest-xdist disables stdout capturing by default, which means that print() statements
    are not captured and displayed in the terminal.
    That's because xdist cannot support -s for technical reasons wrt the process execution mechanism
    https://github.com/pytest-dev/pytest-xdist/issues/354
    """
    original_print = print
    with mock.patch("builtins.print") as mock_print:
        mock_print.side_effect = lambda *args, **kwargs: original_print(*args, **{"file": sys.stderr, **kwargs})
        yield mock_print


@pytest.fixture(scope="session", autouse=True)
def ensure_schemas_exist(spark: SparkSession) -> None:
    for db in DATABASE_NAMES:
        spark.sql(f"CREATE DATABASE IF NOT EXISTS {db}")


@pytest.fixture(scope="session")
def migrations_executed(spark: SparkSession) -> None:
    """Executes all migrations.

    This fixture is useful for all tests that require the migrations to be executed. E.g. when
    a view/dataprodcut/table is required."""

    # Databases are created in dh3infrastructure using terraform
    # So we need to create them in test environment
    for db in DATABASE_NAMES:
        spark.sql(f"CREATE DATABASE IF NOT EXISTS {db}")

    _migrate(SPARK_CATALOG_NAME)
