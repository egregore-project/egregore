// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using egregore.Data;

namespace egregore.Events
{
    public sealed class RecordEvents
    {
        private static readonly IEnumerable<IRecordEventHandler> NoListeners = new List<IRecordEventHandler>(0);

        private readonly IEnumerable<IRecordEventHandler> _listeners;

        public RecordEvents(IEnumerable<IRecordEventHandler> listeners = default)
        {
            _listeners = listeners ?? NoListeners;
        }

        public async Task OnInitAsync(IRecordStore store)
        {
            var pending = new List<Task>();
            foreach (var listener in _listeners)
                pending.Add(listener.OnRecordsInitAsync(store));
            await Task.WhenAll(pending);
        }
        
        public async Task OnAddedAsync(IRecordStore store, Record record)
        {
            var pending = new List<Task>();
            foreach (var listener in _listeners)
                pending.Add(listener.OnRecordAddedAsync(store, record));
            await Task.WhenAll(pending);
        }
    }
}