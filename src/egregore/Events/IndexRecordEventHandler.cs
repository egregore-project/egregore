// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using egregore.Data;
using Microsoft.Extensions.Logging;

namespace egregore.Events
{
    internal sealed class IndexRecordEventHandler : IRecordEventHandler
    {
        private readonly IRecordIndex _index;
        private readonly ILogger<IndexRecordEventHandler> _logger;
        
        public IndexRecordEventHandler(IRecordIndex index, ILogger<IndexRecordEventHandler> logger)
        {
            _index = index;
            _logger = logger;
        }

        public async Task OnRecordsInitAsync(IRecordStore store)
        {
            await _index.RebuildAsync(store);
        }

        public async Task OnRecordAddedAsync(IRecordStore store, Record record)
        {
            await _index.RebuildAsync(store);
        }
    }
}