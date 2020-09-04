// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using egregore.Extensions;

namespace egregore.Media
{
    public sealed class MediaEvents
    {
        private static readonly IReadOnlyList<IMediaEventHandler> NoHandlers = new List<IMediaEventHandler>(0);

        private readonly IEnumerable<IMediaEventHandler> _listeners;

        public MediaEvents(IEnumerable<IMediaEventHandler> listeners = default) => _listeners = listeners ?? NoHandlers;
        
        public Task OnAddedAsync(IMediaStore store, MediaEntry entry, CancellationToken cancellationToken = default) => _listeners.OnMediaAddedAsync(store, entry, cancellationToken);
    }
}