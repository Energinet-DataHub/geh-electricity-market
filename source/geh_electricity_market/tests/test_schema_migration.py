import pytest
from geh_common.data_products.electricity_market_measurements_input import (
    electrical_heating_child_metering_points_v1,
    electrical_heating_consumption_metering_point_periods_v1,
    missing_measurements_log_metering_point_periods_v1,
    net_consumption_group_6_child_metering_points_v1,
    net_consumption_group_6_consumption_metering_point_periods_v1,
)
from geh_common.testing.dataframes.assert_schemas import assert_schema

from tests import SPARK_CATALOG_NAME


@pytest.mark.parametrize(
    "dataproduct",
    [
        capacity_metering_point_periods_v1,
        electrical_heating_child_metering_points_v1,
        electrical_heating_consumption_metering_point_periods_v1,
        missing_measurements_log_metering_point_periods_v1,
        net_consumption_group_6_consumption_metering_point_periods_v1,
        net_consumption_group_6_child_metering_points_v1,
    ],
)
def test_schema_migration(spark, migrations_executed, dataproduct):
    fqn = f"{SPARK_CATALOG_NAME}.{dataproduct.database_name}.{dataproduct.view_name}"
    df = spark.read.table(fqn)
    assert_schema(df.schema, dataproduct.schema, ignore_column_order=True, ignore_nullability=True)
