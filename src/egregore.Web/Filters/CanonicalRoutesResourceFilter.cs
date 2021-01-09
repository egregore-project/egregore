// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace egregore.Web.Filters
{
    public sealed class CanonicalRoutesResourceFilter : IResourceFilter
    {
        private const string SchemeDelimiter = "://";
        private const char ForwardSlash = '/';

        private readonly IOptionsSnapshot<RouteOptions> _options;

        public CanonicalRoutesResourceFilter(IOptionsSnapshot<RouteOptions> options) => _options = options;

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (!string.Equals(context.HttpContext.Request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
                return;

            if (context.ActionDescriptor.EndpointMetadata.Any(x => x is StaticFileRouteAttribute))
                return;

            if (!TryGetCanonicalRoute(context.HttpContext.Request, _options.Value, out var redirectToUrl))
                context.Result = new RedirectResult(redirectToUrl, true, true);
        }

        public void OnResourceExecuted(ResourceExecutedContext context) { }

        internal static bool TryGetCanonicalRoute(HttpRequest request, RouteOptions options, out string redirectToUrl)
        {
            var canonical = true;

            var sb = new StringBuilder();
            if (options.LowercaseUrls)
            {
                AppendLowercase(sb, request.Scheme, ref canonical);
            }
            else
            {
                sb.Append(request.Scheme);
            }

            sb.Append(SchemeDelimiter);

            if (request.Host.HasValue)
            {
                if (options.LowercaseUrls)
                {
                    AppendLowercase(sb, request.Host.Value, ref canonical);
                }
                else
                {
                    sb.Append(request.Host);
                }
            }

            if (request.PathBase.HasValue)
            {
                if (options.LowercaseUrls)
                {
                    AppendLowercase(sb, request.PathBase.Value, ref canonical);
                }
                else
                {
                    sb.Append(request.PathBase);
                }

                if (options.AppendTrailingSlash && !request.Path.HasValue)
                {
                    if (request.PathBase.Value != null && request.PathBase.Value[^1] != ForwardSlash)
                    {
                        sb.Append(ForwardSlash);
                        canonical = false;
                    }
                }
            }

            if (request.Path.HasValue)
            {
                if (options.LowercaseUrls)
                {
                    AppendLowercase(sb, request.Path.Value, ref canonical);
                }
                else
                {
                    sb.Append(request.Path);
                }

                if (options.AppendTrailingSlash)
                {
                    if (request.Path.Value != null && request.Path.Value[^1] != ForwardSlash)
                    {
                        sb.Append(ForwardSlash);
                        canonical = false;
                    }
                }
            }

            if (request.QueryString.HasValue)
            {
                if (options.LowercaseUrls && options.LowercaseQueryStrings)
                {
                    AppendLowercase(sb, request.QueryString.Value, ref canonical);
                }
                else
                {
                    sb.Append(request.QueryString);
                }
            }

            redirectToUrl = canonical ? null : sb.ToString();

            return canonical;
        }

        private static void AppendLowercase(StringBuilder sb, string value, ref bool valid)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsUpper(value, i))
                {
                    valid = false;
                    sb.Append(char.ToLowerInvariant(value[i]));
                }
                else
                {
                    sb.Append(value[i]);
                }
            }
        }
    }
}