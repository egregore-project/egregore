// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using egregore.Cryptography;
using egregore.Data;
using egregore.Data.Tests;
using egregore.Data.Tests.Fixtures;
using egregore.Models;
using egregore.Models.Exceptions;
using egregore.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    [Collection("Serial")]
    public class OntologyTests
    {
        public OntologyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        [Fact]
        public async Task Can_rehydrate_ontology_from_log_stream()
        {
            const string ns = "MyApp";

            using var fixture = new LogStoreFixture();

            var @default = LogEntryFactory.CreateNamespaceEntry(Constants.DefaultNamespace, default);
            await fixture.Store.AddEntryAsync(@default);

            var @namespace = LogEntryFactory.CreateNamespaceEntry(ns, @default.Hash);
            await fixture.Store.AddEntryAsync(@namespace);

            var schema = new Schema {Name = "Customer"};
            schema.Properties.Add(new SchemaProperty {Name = "Name", Type = "string"});
            await fixture.Store.AddEntryAsync(LogEntryFactory.CreateEntry(schema, @namespace.Hash));
            
            byte[] pk;
            unsafe
            {
                Crypto.GenerateKeyPair(out pk, out _);
            }

            var ontology = new MemoryOntologyLog(new OntologyEvents(), pk);
            await ontology.MaterializeAsync(fixture.Store);

            Assert.Equal(2, ontology.Namespaces.Count);
            Assert.Equal(Constants.DefaultNamespace, ontology.Namespaces[0].Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(ns, ontology.Namespaces[1].Value, StringComparer.OrdinalIgnoreCase);

            Assert.Single(ontology.Roles[Constants.DefaultNamespace]);
            Assert.Empty(ontology.Roles[ns]);
        }

        [Fact]
        public async Task Cannot_revoke_only_owner_grant()
        {
            var capture = new PlaintextKeyCapture("rosebud", "rosebud");
            var service = new TempKeyFileService();
            var publicKey = CryptoTestHarness.GenerateKeyFile(_output, capture, service);
            capture.Reset();

            var revoke = new RevokeRole(Constants.DefaultOwnerRole, publicKey, publicKey);
            revoke.Sign(service, capture);
            Assert.True(revoke.Verify(), "revocation did not verify");

            using var fixture = new LogStoreFixture();

            var @default = LogEntryFactory.CreateNamespaceEntry(Constants.DefaultNamespace, default);
            await fixture.Store.AddEntryAsync(@default);

            var ontology = new MemoryOntologyLog(new OntologyEvents(), publicKey);
            Assert.Single(ontology.Roles[Constants.DefaultNamespace]);

            await fixture.Store.AddEntryAsync(LogEntryFactory.CreateEntry(revoke));
            await Assert.ThrowsAsync<CannotRemoveSingleOwnerException>(() => ontology.MaterializeAsync(fixture.Store));
        }

        [Fact]
        public void Empty_ontology_has_default_namespace()
        {
            unsafe
            {
                Crypto.GenerateKeyPair(out var pk, out _);
                var ontology = new MemoryOntologyLog(new OntologyEvents(), pk);
                Assert.Single(ontology.Namespaces);
                Assert.Equal("default", ontology.Namespaces[0].Value, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}