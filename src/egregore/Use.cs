// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using egregore.Configuration;
using egregore.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal static class Use
    {
        private static readonly string[] BlazorPaths =
            {"/", "/counter", "/fetchdata", "/meta", "/privacy", "/upload", "/editor", "/logs", "/metrics"};

        public static IApplicationBuilder UseWebServer(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors();

            // manually instanced singletons are not cleaned up by DI on application exit
            app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
                app.ApplicationServices.GetService<IPersistedKeyCapture>()?.Dispose());

            if (env.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseHsts();
            }

            // See: https://tools.ietf.org/html/rfc7807
            app.UseExceptionHandler(a =>
            {
                // FIXME: Use this for pages, and the default handler if not on a page
                // app.UseExceptionHandler("/error");
                // app.UseStatusCodePagesWithReExecute("/error/{0}");

                a.Run(async context =>
                {
                    var handler = context.Features.Get<IExceptionHandlerFeature>();
                    var detail =  handler.Error.ToString();
                    var problemDetails = new ProblemDetails
                    {
                        Title = "An unexpected error occurred!",
                        Status = 500,
                        Detail = detail,
                        Instance = $"urn:myorganization:error:{Guid.NewGuid()}"
                    };

                    context.Response.StatusCode = 500;
                    context.Response.ContentType = Constants.MediaTypeNames.Application.ProblemJson;
                    var options = new JsonSerializerOptions();
                    await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, options, context.RequestAborted);
                });
            });

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".webmanifest"] = "application/manifest+json";
            app.UseStaticFiles(new StaticFileOptions {ContentTypeProvider = provider});

            app.UseSecurityHeaders();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<NotificationHub>("/notify");
                endpoints.MapHub<LiveQueryHub>("/query");

                endpoints.MapControllers();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();

                //endpoints.MapFallbackToFile("index.html");
                endpoints.MapGet("/",CreateRequestDelegate(endpoints, "index.html"));
            });

            return app;
        }

        private static RequestDelegate CreateRequestDelegate(IEndpointRouteBuilder endpoints, string filePath)
        {
            var app = endpoints.CreateApplicationBuilder();
            app.Use(next => context =>
            {
                context.Request.Path = "/" + filePath;

                // Set endpoint to null so the static files middleware will handle the request.
                context.SetEndpoint(null);

                return next(context);
            });

            app.UseStaticFiles();
            return app.Build();
        }

        public static void UseSecurityHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                if (!IsApiPath(context) && !IsBlazorPath(context))
                {
                    if (context.Request.Method == HttpMethods.Get)
                        context.Response.Headers.TryAdd(Constants.HeaderNames.ContentSecurityPolicy,
                            "default-src 'self'");

                    var options = context.RequestServices.GetService<IOptionsSnapshot<WebServerOptions>>();
                    if (options != default && options.Value.PublicKey.Length > 0)
                    {
                        var keyPin = Convert.ToBase64String(Crypto.Sha256(options.Value.PublicKey));
                        context.Response.Headers.TryAdd(Constants.HeaderNames.PublicKeyPins,
                            $"pin-sha256=\"{keyPin}\"; max-age={TimeSpan.FromDays(7).Seconds}; includeSubDomains");
                    }

                    context.Response.Headers.TryAdd(Constants.HeaderNames.XFrameOptions, "DENY");
                    context.Response.Headers.TryAdd(Constants.HeaderNames.XContentTypeOptions, "nosniff");
                    context.Response.Headers.TryAdd(Constants.HeaderNames.ReferrerPolicy, "no-referrer");
                    context.Response.Headers.TryAdd(Constants.HeaderNames.PermissionsPolicy, "unsized-media 'self'");
                }

                await next();
            });
        }

        private static bool IsBlazorPath(HttpContext context)
        {
            return BlazorPaths.Any(x => x == context.Request.Path);
        }

        private static bool IsApiPath(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/api");
        }
    }
}