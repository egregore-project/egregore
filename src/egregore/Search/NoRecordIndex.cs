// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using Lunr;

namespace egregore.Search
{
    internal sealed class NoRecordIndex : IRecordIndex
    {
        public Task RebuildAsync(IRecordStore store, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<RecordSearchResult> SearchAsync(string query,
            CancellationToken cancellationToken = default)
        {
            return AsyncEnumerableExtensions.Empty<RecordSearchResult>();
        }
    }
}