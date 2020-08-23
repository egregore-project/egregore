// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using egregore.Caching;
using egregore.Data;

namespace egregore.Events
{
    internal sealed class InvalidateCacheRecordEventHandler : IRecordEventHandler
    {
        private readonly ICacheRegion<SyndicationFeed> _cache;

        public InvalidateCacheRecordEventHandler(ICacheRegion<SyndicationFeed> cache)
        {
            _cache = cache;
        }

        public Task OnRecordsInitAsync(IRecordStore store) => Task.CompletedTask;

        public Task OnRecordAddedAsync(IRecordStore store, Record record)
        {
            _cache.Clear();
            return Task.CompletedTask;
        }
    }
}