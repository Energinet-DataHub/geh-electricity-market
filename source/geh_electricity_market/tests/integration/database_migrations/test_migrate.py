import pytest
from pyspark.sql import SparkSession

from geh_electricity_market.database_migrations import migrations_runner
from geh_electricity_market.database_migrations.entry_point import migrate
from geh_electricity_market.testing.utilities.create_azure_log_query_runner import (
    LogsQueryStatus,
    create_azure_log_query_runner,
)


def test__when_running_migrate__then_log_is_produced(spark: SparkSession, monkeypatch: pytest.MonkeyPatch):
    # Arrange
    azure_query_runnner = create_azure_log_query_runner(monkeypatch)
    timeout_minutes = 15

    monkeypatch.setattr(
        migrations_runner, "_migrate", lambda name, subs: None
    )  # Mock this function to avoid actual migration

    # Act
    expected_log_messages = [
        "Initializing migrations with:\\nLogging Settings:",
        "Initializing migrations with:\\nCatalog Settings:",
    ]
    migrate()

    # Assert
    for message in expected_log_messages:
        query = f"""
            AppTraces
            | where Properties.Subsystem == "electricity-market"
            | where AppRoleName == "dbr-electricity-market"
            | where Message startswith_cs "{message}"
            | where TimeGenerated > ago({timeout_minutes}m)
        """

        query_result = azure_query_runnner(query, timeout_minutes=timeout_minutes)
        assert query_result.status == LogsQueryStatus.SUCCESS, f"The query did not complete successfully:\n{query}"
        assert query_result.tables[0].rows, (
            f"No logs were found for the given query:\n{query}\n---\n{query_result.tables}"
        )
