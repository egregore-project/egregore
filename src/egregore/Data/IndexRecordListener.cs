// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace egregore.Data
{
    internal sealed class IndexRecordListener : IRecordListener
    {
        private readonly IRecordIndex _index;
        private readonly ILogger<IndexRecordListener> _logger;
        
        public IndexRecordListener(IRecordIndex index, ILogger<IndexRecordListener> logger)
        {
            _index = index;
            _logger = logger;
        }

        public async Task OnRecordsInitAsync()
        {
            await _index.RebuildAsync();
        }

        public async Task OnRecordAddedAsync(Record record)
        {
            await _index.RebuildAsync();
        }
    }
}