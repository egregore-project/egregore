// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Web.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace egregore.Web
{
    public static class PublicFilters
    {
        public static IServiceCollection AddPublicFilters(this IServiceCollection services)
        {
            services.AddSingleton<ThrottleFilter>();
            services.AddSingleton<RemoteAddressFilter>();
            services.AddSingleton<OntologyFilter>();
            return services;
        }
    }
}