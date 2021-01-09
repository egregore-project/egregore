// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace egregore.Web
{
    public static class CacheRegions
    {
        public static IServiceCollection AddCacheRegions(this IServiceCollection services)
        {
            services.AddMemoryCache(x => { });
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ICacheRegion<>), typeof(InProcessCacheRegion<>)));
            return services;
        }
    }
}