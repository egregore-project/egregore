// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using System.Threading.Tasks;
using egregore.Ontology;
using Xunit;

namespace egregore.Tests.Ontology
{
    public class LogStoreTests
    {
        [Fact]
        public async Task Can_append_namespace_entry_to_store_and_load_from_stream()
        {
            const string ns = "MyApp";

            using var fixture = new LogStoreFixture();
            var entry = LogEntryFactory.CreateEntry(new Namespace(ns));

            var count = await fixture.Store.AddEntryAsync(entry);
            Assert.Equal(1UL, count);
            Assert.Equal(0UL, entry.Index);

            var items = 0;
            foreach (var item in fixture.Store.StreamEntries())
            foreach (var @object in item.Objects)
            {
                Assert.True(@object.Data is Namespace);
                Assert.Equal(ns, ((Namespace) @object.Data).Value);
                Assert.Equal(LogEntryFactory.TypeProvider.Get(typeof(Namespace)), @object.Type);
                Assert.Equal(LogSerializeContext.FormatVersion, @object.Version);
                items++;
            }

            Assert.Equal(1, items);
        }


        [Fact]
        public async Task Can_stream_valid_entries()
        {
            const string ns = "MyApp";

            using var fixture = new LogStoreFixture();

            var one = LogEntryFactory.CreateEntry(new Namespace(ns));
            var two = LogEntryFactory.CreateEntry(new Namespace(ns), one.Hash);
            Assert.False(one.Hash.SequenceEqual(two.Hash));
            Assert.True(one.Hash.SequenceEqual(two.PreviousHash));

            await fixture.Store.AddEntryAsync(one);
            Assert.Equal(0UL, one.Index);

            await fixture.Store.AddEntryAsync(two);
            Assert.Equal(1UL, two.Index);

            var count = await fixture.Store.GetLengthAsync();
            Assert.Equal(2UL, count);

            var items = 0;
            foreach (var item in fixture.Store.StreamEntries())
            foreach (var @object in item.Objects)
            {
                Assert.True(@object.Data is Namespace);
                Assert.Equal(ns, ((Namespace) @object.Data).Value);
                Assert.Equal(LogEntryFactory.TypeProvider.Get(typeof(Namespace)), @object.Type);
                Assert.Equal(LogSerializeContext.FormatVersion, @object.Version);
                items++;
            }

            Assert.Equal(2, items);
        }

        [Fact]
        public async Task Empty_store_has_zero_length()
        {
            using var fixture = new LogStoreFixture();
            var count = await fixture.Store.GetLengthAsync();
            Assert.Equal(0UL, count);
        }
    }
}