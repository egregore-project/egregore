// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace egregore
{
    public sealed class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(x => x.AddDefaultPolicy(b =>
            {
                b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                b.DisallowCredentials(); // credentials are invalid when origin is *
            }));

            services.AddSignalR();

            services.AddEvents();
            services.AddDataStores();
            services.AddFilters();
            services.AddDaemonServices();

            services.AddRouting(x =>
            {
                x.AppendTrailingSlash = true;
                x.LowercaseUrls = true;
                x.LowercaseQueryStrings = false;
            });

            services.AddDynamicControllers(Environment);
            services.AddCacheRegions();
            services.AddIdentity();
            services.AddSearchIndexes();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWebServer(env);
        }
    }
}