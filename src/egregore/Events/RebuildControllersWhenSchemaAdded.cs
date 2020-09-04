// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Ontology;

namespace egregore.Events
{
    internal sealed class RebuildControllersWhenSchemaAdded : SchemaAddedEventHandler
    {
        private readonly DynamicActionDescriptorChangeProvider _changeProvider;

        public RebuildControllersWhenSchemaAdded(DynamicActionDescriptorChangeProvider changeProvider) => _changeProvider = changeProvider;
        public override Task OnSchemaAddedAsync(ILogStore store, Schema schema, CancellationToken cancellationToken = default) => Task.Run(() => _changeProvider.OnChanged(), cancellationToken);
    }
}