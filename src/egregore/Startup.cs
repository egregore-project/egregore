// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text.Json;
using egregore.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            app.UseCors();

            // manually instanced singletons are not cleaned up by DI on application exit
            app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
                app.ApplicationServices.GetService<IPersistedKeyCapture>()?.Dispose());

            if (env.IsDevelopment())
                app.UseWebAssemblyDebugging();
            else
                app.UseHsts();

            // See: https://tools.ietf.org/html/rfc7807
            app.UseExceptionHandler(a =>
            {
                // FIXME: Use this for pages, and the default handler if not on a page
                // app.UseExceptionHandler("/error");
                // app.UseStatusCodePagesWithReExecute("/error/{0}");

                a.Run(async context =>
                {
                    var handler = context.Features.Get<IExceptionHandlerFeature>();
                    var detail = handler.Error.ToString();
                    var problemDetails = new ProblemDetails
                    {
                        Title = "An unexpected error occurred!",
                        Status = 500,
                        Detail = detail,
                        Instance = $"urn:egregore:error:{Guid.NewGuid()}"
                    };

                    context.Response.StatusCode = 500;
                    context.Response.ContentType = Constants.MediaTypeNames.Application.ProblemJson;
                    var options = new JsonSerializerOptions();
                    await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, options,
                        context.RequestAborted);
                });
            });

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".webmanifest"] = "application/manifest+json";
            app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });
            
            app.UseSecurityHeaders();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHub<NotificationHub>("/notify");
                endpoints.MapHub<LiveQueryHub>("/query");
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}