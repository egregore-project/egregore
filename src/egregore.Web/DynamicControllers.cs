// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Data;
using egregore.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace egregore.Web
{
    public static class DynamicControllers
    {
        public static IServiceCollection AddDynamicControllers(this IServiceCollection services,
            IWebHostEnvironment env)
        {
            var change = new DynamicActionDescriptorChangeProvider();
            services.AddSingleton(change);
            services.AddSingleton<IActionDescriptorChangeProvider, DynamicActionDescriptorChangeProvider>(r =>
                r.GetRequiredService<DynamicActionDescriptorChangeProvider>());

            // FIXME: bad practice
            var sp = services.BuildServiceProvider();

            var mvc = services.AddControllersWithViews(x =>
            {
                x.Conventions.Add(new DynamicControllerModelConvention());
            });

            mvc.ConfigureApplicationPartManager(x =>
            {
                var logger = sp.GetRequiredService<ILogger<DynamicControllerFeatureProvider>>();
                var ontology = sp.GetRequiredService<IOntologyLog>();
                var provider = new DynamicControllerFeatureProvider(ontology, logger);
                x.FeatureProviders.Add(provider);
            });

            if (env.IsDevelopment()) mvc.AddRazorRuntimeCompilation(o => { });

            services.AddRazorPages();

            return services;
        }
    }
}