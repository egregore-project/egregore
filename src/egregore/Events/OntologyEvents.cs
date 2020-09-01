// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using egregore.Extensions;
using egregore.Ontology;

namespace egregore.Events
{
    public sealed class OntologyEvents
    {
        private static readonly IReadOnlyList<IOntologyEventHandler> NoHandlers = new List<IOntologyEventHandler>(0);
        private readonly IEnumerable<IOntologyEventHandler> _handlers;
        public OntologyEvents(IEnumerable<IOntologyEventHandler> handlers = default) => _handlers = handlers ?? NoHandlers;
        public Task OnSchemaAddedAsync(ILogStore store, Schema schema, CancellationToken cancellationToken = default) => _handlers.OnSchemaAddedAsync(store, schema, cancellationToken);
    }
}