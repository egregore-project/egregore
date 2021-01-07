// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using Microsoft.Extensions.Logging;

namespace egregore.Events
{
    internal sealed class RebuildIndexOnRecordEvents : IRecordEventHandler
    {
        private readonly ISearchIndex _index;
        private readonly ILogger<RebuildIndexOnRecordEvents> _logger;

        public RebuildIndexOnRecordEvents(ISearchIndex index, ILogger<RebuildIndexOnRecordEvents> logger)
        {
            _index = index;
            _logger = logger;
        }

        public Task OnRecordsInitAsync(IRecordStore store, CancellationToken cancellationToken = default)
        {
            return _index.RebuildAsync(store, cancellationToken);
        }

        public Task OnRecordAddedAsync(IRecordStore store, Record record, CancellationToken cancellationToken = default)
        {
            return _index.RebuildAsync(store, cancellationToken);
        }
    }
}