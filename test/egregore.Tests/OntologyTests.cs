// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Schema;
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
            Assert.Equal("default", ontology.Namespaces[0], StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Can_rehydrate_ontology_from_log_stream()
        {
            var ontology = new OntologyLog();
            Assert.Single(ontology.Namespaces);
            Assert.Equal("default", ontology.Namespaces[0], StringComparer.OrdinalIgnoreCase);
        }
    }
}