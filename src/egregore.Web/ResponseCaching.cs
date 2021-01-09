// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace egregore.Web
{
    public static class ResponseCaching
    {
        public static IServiceCollection AddMetadataCaching(this IServiceCollection services)
        {
            services.AddMvcCore(o =>
            {
                o.CacheProfiles.Add(Constants.DailyCacheProfileName, new CacheProfile {Duration = Constants.OneDayInSeconds});
                o.CacheProfiles.Add(Constants.YearlyCacheProfileName, new CacheProfile {Duration = Constants.OneYearInSeconds});
            });
            return services;
        }
    }
}