// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using egregore.Ontology;
using egregore.Ontology.Exceptions;
using Xunit;

namespace egregore.Tests
{
    public class OntologyTests
    {
        [Fact]
        public void Empty_ontology_has_default_namespace()
        {
            var owner = Crypto.GenerateKeyPairDangerous();
            var ontology = new OntologyLog(owner.publicKey);
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
            await fixture.Store.AddEntryAsync(LogEntryFactory.CreateEntry(schema, @namespace.Hash));

            var owner = Crypto.GenerateKeyPairDangerous();
            var ontology = new OntologyLog(owner.publicKey, fixture.Store);
            Assert.Equal(2, ontology.Namespaces.Count);
            Assert.Equal(Constants.DefaultNamespace, ontology.Namespaces[0].Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(ns, ontology.Namespaces[1].Value, StringComparer.OrdinalIgnoreCase);

            Assert.Single(ontology.Roles[Constants.DefaultNamespace]);
            Assert.Empty(ontology.Roles[ns]);
        }

        [Fact]
        public async Task Cannot_revoke_only_owner_grant()
        {
            var publicKey = CryptoTests.GenerateSecretKeyOnDisk(out var fileName);

            using var fixture = new LogStoreFixture();

            var ontology = new OntologyLog(publicKey);
            Assert.Single(ontology.Roles[Constants.DefaultNamespace]);

            var revoke = new RevokeRole(Constants.OwnerRole, publicKey, publicKey);
            revoke.Sign(File.OpenRead(fileName));
            
            await fixture.Store.AddEntryAsync(LogEntryFactory.CreateEntry(revoke));
            Assert.Throws<CannotRemoveSingleOwnerException>(() => { ontology.Materialize(fixture.Store); });
        }
    }
}