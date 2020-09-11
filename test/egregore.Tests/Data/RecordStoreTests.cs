// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using Dapper;
using egregore.Data;
using egregore.Tests.Helpers;
using Xunit;
using Record = egregore.Data.Record;

namespace egregore.Tests.Data
{
    [Collection("Serial")]
    public class RecordStoreTests
    {
        [Fact]
        public async Task Can_append_record_and_retrieve_by_uuid()
        {
            using var fixture = new RecordStoreFixture();

            var record = new Record {Type = "Customer"};
            record.Columns.Add(new RecordColumn(0, "Order", "int", "123", "0"));

            var next = await fixture.Store.AddRecordAsync(record);
            Assert.Equal(0UL, record.Index);
            Assert.Equal(1UL, next);

            var count = await fixture.Store.GetLengthByTypeAsync("Customer");
            Assert.Equal(1UL, count);

            var fetched = await fixture.Store.GetByIdAsync(record.Uuid);
            Assert.NotNull(fetched);

            var badId = Guid.NewGuid();
            var missing = await fixture.Store.GetByIdAsync(badId);
            Assert.Null(missing);
        }

        [Fact]
        public async Task Can_search_for_record_by_column_value()
        {
            using var fixture = new RecordStoreFixture();

            var record = new Record {Type = "Customer"};
            record.Columns.Add(new RecordColumn(0, "Order", "int", "123", "0"));

            var next = await fixture.Store.AddRecordAsync(record);
            Assert.Equal(0UL, record.Index);
            Assert.Equal(1UL, next);

            var count = await fixture.Store.GetLengthByTypeAsync("Customer");
            Assert.Equal(1UL, count);

            var fetched = await fixture.Store.GetByColumnValueAsync("Customer", "Order", "123");
            var list = fetched.AsList();
            Assert.NotEmpty(list);

            var missing = await fixture.Store.GetByColumnValueAsync("Customer", "Order", "234");
            list = missing.AsList();
            Assert.Empty(list);
        }

        [Fact]
        public async Task Empty_store_has_zero_length()
        {
            using var fixture = new RecordStoreFixture();
            var count = await fixture.Store.GetLengthAsync();
            Assert.Equal(0UL, count);
        }
    }
}