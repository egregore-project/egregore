// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using egregore.Ontology;
using Xunit;

namespace egregore.Tests
{
    public class OntologyTests
    {
        [Fact]
        public void Empty_ontology_has_default_namespace()
        {
            var ontology = new OntologyLog();
            Assert.Single(ontology.Namespaces);
            Assert.Equal("default", ontology.Namespaces[0].Value, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Can_rehydrate_ontology_from_log_stream()
        {
            const string ns = "MyApp";

            using var fixture = new LogStoreFixture();

            var @namespace = LogEntryFactory.CreateNamespaceEntry(ns, default);
            await fixture.Store.AddEntryAsync(@namespace);

            var schema = new Schema { Name = "Customer" };
            schema.Properties.Add(new SchemaProperty { Name = "Name", Type = "string" });
            await fixture.Store.AddEntryAsync(LogEntryFactory.CreateSchemaEntry(schema, @namespace.Hash));

            var ontology = new OntologyLog(fixture.Store);
            Assert.Equal(2, ontology.Namespaces.Count);
            Assert.Equal(Constants.DefaultNamespace, ontology.Namespaces[0].Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(ns, ontology.Namespaces[1].Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}