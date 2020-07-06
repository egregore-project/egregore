// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using egregore.Ontology;
using egregore.Ontology.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public class OntologyTests
    {
        private readonly ITestOutputHelper _output;

        public OntologyTests(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public void Empty_ontology_has_default_namespace()
        {
            unsafe
            {
                Crypto.GenerateKeyPair(out var pk, out _);
                var ontology = new OntologyLog(pk);
                Assert.Single(ontology.Namespaces);
                Assert.Equal("default", ontology.Namespaces[0].Value, StringComparer.OrdinalIgnoreCase);
            }
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
            await fixture.Store.AddEntryAsync(LogEntryFactory.CreateEntry(schema, @namespace.Hash));

            unsafe
            {
                Crypto.GenerateKeyPair(out var pk, out _);
                var ontology = new OntologyLog(pk, fixture.Store);
                Assert.Equal(2, ontology.Namespaces.Count);
                Assert.Equal(Constants.DefaultNamespace, ontology.Namespaces[0].Value, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(ns, ontology.Namespaces[1].Value, StringComparer.OrdinalIgnoreCase);

                Assert.Single(ontology.Roles[Constants.DefaultNamespace]);
                Assert.Empty(ontology.Roles[ns]);
            }
        }

        [Fact]
        public async Task Cannot_revoke_only_owner_grant()
        {
            var capture = new TestKeyCapture("rosebud", "rosebud");
            var service = new TestKeyFileService();
            var publicKey = CryptoTestHarness.GenerateKeyFile(_output, capture, service);
            capture.Reset();

            var revoke = new RevokeRole(Constants.OwnerRole, publicKey, publicKey);
            revoke.Sign(service, capture);
            Assert.True(revoke.Verify(), "revocation did not verify");

            using var fixture = new LogStoreFixture();

            var ontology = new OntologyLog(publicKey);
            Assert.Single(ontology.Roles[Constants.DefaultNamespace]);

            await fixture.Store.AddEntryAsync(LogEntryFactory.CreateEntry(revoke));
            Assert.Throws<CannotRemoveSingleOwnerException>(() => { ontology.Materialize(fixture.Store); });
        }
    }
}