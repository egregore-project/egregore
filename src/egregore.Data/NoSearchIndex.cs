﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lunr;

namespace egregore.Data
{
    public sealed class NoSearchIndex : ISearchIndex
    {
        public Task RebuildAsync(IRecordStore store, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<SearchResult> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            return AsyncEnumerableExtensions.Empty<SearchResult>();
        }
    }
}