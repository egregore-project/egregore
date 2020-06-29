using Dapper;
using egregore.Data;
using Xunit;

using Record = egregore.Data.Record;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace egregore.Tests.Data
{
    public class DataProjectionTests
    {
        [Fact]
        public void Can_project_data_structure_changes_over_time()
        {
            var fixture = new SqliteProjectionFixture();

            // CustomerV1 (Order INT)
            Create_customer_v1_record(fixture);

            // CustomerV2 (Name TEXT, Order INT)
            Create_customer_v2_record(fixture);

            // CustomerV3 (Name TEXT)
            Create_customer_v3_record(fixture);

            // CustomerV4 (Name TEXT, Order TEXT)
            Create_customer_v4_record(fixture);
        }
        
        #region CustomerV1

        private static void Create_customer_v1_record(SqliteProjectionFixture fixture)
        {
            var record = new Record {Type = "Customer"};
            record.Columns.Add(new RecordColumn(0, "Order", "int", "123"));

            fixture.Projection.Visit(record);

            using var db = fixture.Projection.OpenConnection();
            var customers = db.Query<CustomerV1>("SELECT * FROM 'Customer_V1'").AsList();
            Assert.Single(customers);
            Assert.Equal(123, customers[0].Order);
        }

        private class CustomerV1
        {
            public int Order { get; set; }
        }

        #endregion

        #region CustomerV2 (add a column)

        private static void Create_customer_v2_record(SqliteProjectionFixture fixture)
        {
            var record = new Record {Type = "Customer"};
            record.Columns.Add(new RecordColumn(0, "Name", "string", "Bobby Tables") {Default = "ABC"});
            record.Columns.Add(new RecordColumn(1, "Order", "int", "456"));

            fixture.Projection.Visit(record);

            using var db = fixture.Projection.OpenConnection();
            
            var v2 = db.Query<CustomerV2>("SELECT * FROM \"Customer\"").AsList();
            Assert.Equal(2, v2.Count);
            Assert.Equal(123, v2[0].Order);
            Assert.Equal("ABC", v2[0].Name);
            Assert.Equal(456, v2[1].Order);
            Assert.Equal("Bobby Tables", v2[1].Name);
        }

        private class CustomerV2
        {
            public string Name { get; set; }
            public int Order { get; set; }
        }

        #endregion

        #region CustomerV3 (drop a column)

        private static void Create_customer_v3_record(SqliteProjectionFixture fixture)
        {
            var record = new Record {Type = "Customer"};
            record.Columns.Add(new RecordColumn(0, "Name", "string", "Bobby Fables") {Default = "ABC"});

            fixture.Projection.Visit(record);

            using var db = fixture.Projection.OpenConnection();

            var v3 = db.Query<CustomerV3>("SELECT * FROM 'Customer'").AsList();
            Assert.Equal(3, v3.Count);
            Assert.Equal("ABC", v3[0].Name);
            Assert.Equal("Bobby Fables", v3[1].Name);
            Assert.Equal("Bobby Tables", v3[2].Name);
        }

        private class CustomerV3
        {
            public string Name { get; set; }
        }

        #endregion

        #region CustomerV4 (add back a previously added column with a changed type)

        private static void Create_customer_v4_record(SqliteProjectionFixture fixture)
        {
            var record = new Record {Type = "Customer"};
            record.Columns.Add(new RecordColumn(0, "Name", "string", "Bobby Cables") {Default = "ABC"});
            record.Columns.Add(new RecordColumn(0, "Order", "string", "ABC123") {Default = "XYZ000"});

            fixture.Projection.Visit(record);

            using var db = fixture.Projection.OpenConnection();

            var v4 = db.Query<CustomerV4>("SELECT \"Name\", \"Order\" FROM 'Customer'").AsList();
            Assert.Equal(4, v4.Count);
            Assert.Equal("ABC", v4[0].Name);
            Assert.Equal("123", v4[0].Order);

            Assert.Equal("Bobby Cables", v4[1].Name);
            Assert.Equal("ABC123", v4[1].Order);

            Assert.Equal("Bobby Fables", v4[2].Name);
            Assert.Equal("XYZ000", v4[2].Order);

            Assert.Equal("Bobby Tables", v4[3].Name);
            Assert.Equal("456", v4[3].Order);
        }

        private class CustomerV4
        {
            public string Name { get; set; }
            public string Order { get; set; }
        }

        #endregion
    }
}
