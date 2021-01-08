// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using egregore.Configuration;
using egregore.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal static class Use
    {
        private static readonly string[] BlazorPages = {"/", "/counter", "/fetchdata", "/meta", "/privacy", "/upload", "/editor", "/logs", "/metrics"};
        
        private static bool IsBlazorPath(HttpContext context) => BlazorPages.Any(x => x == context.Request.Path);
        private static bool IsApiPath(HttpContext context) => context.Request.Path.StartsWithSegments("/api");
        
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
    }
}