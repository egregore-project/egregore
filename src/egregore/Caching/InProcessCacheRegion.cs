// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Reflection;
using egregore.Data;
using Microsoft.Extensions.Caching.Memory;

namespace egregore.Caching
{
    internal sealed class InProcessCacheRegion<TRegion> : ICacheRegion<TRegion>
    {
        private readonly IMemoryCache _cache;
        
        public InProcessCacheRegion(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryGetValue<TValue>(in string key, out TValue value) => _cache.TryGetValue(GetCacheKey(key), out value);
        public TValue Get<TValue>(in string key) => _cache.Get<TValue>(GetCacheKey(key));
        public void Set<TValue>(in string key, TValue value, in TimeSpan ttl) => _cache.Set(GetCacheKey(key), value, TimeZoneLookup.Now.Timestamp.Add(ttl));
        public void Set<TValue>(in string key, TValue value) => _cache.Set(GetCacheKey(key), value);
        
        public void Clear()
        {
            var collection = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!(collection?.GetValue(_cache) is ICollection entries))
                return;

            var prefix = GetCacheKey(string.Empty);

            foreach (var entry in entries)
            {
                var property = entry.GetType().GetProperty(nameof(ICacheEntry.Key));
                var value = property?.GetValue(entry);
                if (!(value is string key))
                    continue;

                if(key.StartsWith(prefix))
                    _cache.Remove(key);
            }
        }
        
        private static string GetCacheKey(string key) => $"{typeof(TRegion).Name}:{key}";
    }
}