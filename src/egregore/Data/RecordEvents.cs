// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using egregore.Extensions;

namespace egregore.Data
{
    public sealed class RecordEvents
    {
        private static readonly IReadOnlyList<IRecordEventHandler> NoHandlers = new List<IRecordEventHandler>(0);

        private readonly IEnumerable<IRecordEventHandler> _listeners;

        public RecordEvents(IEnumerable<IRecordEventHandler> listeners = default)
        {
            _listeners = listeners ?? NoHandlers;
        }

        public Task OnInitAsync(IRecordStore store, CancellationToken cancellationToken = default)
        {
            return _listeners.OnRecordsInitAsync(store, cancellationToken);
        }

        public Task OnAddedAsync(IRecordStore store, Record record, CancellationToken cancellationToken = default)
        {
            return _listeners.OnRecordAddedAsync(store, record, cancellationToken);
        }
    }
}