// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using egregore.Schema;
using Xunit;

namespace egregore.Tests
{
    public class LogStoreTests
    {
        [Fact]
        public async Task Empty_store_has_zero_length()
        {
            using var fixture = new LogStoreFixture();
            var count = await fixture.Store.GetLengthAsync();
            Assert.Equal(0UL, count);
        }

        [Fact]
        public async Task Can_append_namespace_entry_to_store_and_load_from_stream()
        {
            const string ns = "MyApp";

            using var fixture = new LogStoreFixture();
            var entry = LogEntryFactory.CreateNamespaceEntry(ns);
            
            var count = await fixture.Store.AddEntryAsync(entry);
            Assert.Equal(1UL, count);
            Assert.Equal(entry.Index, count);

            var items = 0;
            foreach (var item in fixture.Store.StreamEntries())
            {
                foreach (var @object in item.Objects)
                {
                    Assert.True(@object.Data is Namespace);
                    Assert.Equal(ns, ((Namespace)@object.Data).Value);
                    Assert.Equal(Namespace.Type, @object.Type);
                    Assert.Equal(Namespace.Version, @object.Version);
                    items++;
                }
            }

            Assert.Equal(1, items);
        }

        
    }
}