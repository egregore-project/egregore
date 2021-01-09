// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using WyHash;

namespace egregore.Web.Filters
{
    /// <summary>
    ///     Throttles traffic generically for heavy anonymous operations exposed to public networks.
    ///     <remarks>
    ///         - ASP.NET Core is already exposing the IP address in process memory, so we can't do much about that.
    ///         - We need to hash the IP address to avoid ever storing it in memory outside of what ASP.NET Core is doing.
    ///         - Using the IP address in any other way is a privacy breach.
    ///         - This most likely should be disclosed in any auto-generated privacy policies.
    ///         - Wyhash was chosen due to speed and performance with small keys (i.e. use in a hash table)
    ///     </remarks>
    ///     <seealso href="https://github.com/rurban/smhasher/" />
    ///     <seealso href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After" />
    /// </summary>
    internal sealed class ThrottleFilter : IAsyncActionFilter
    {
        private static readonly ulong Seed = BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(ThrottleFilter)));

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _retryAfterDuration;

        public ThrottleFilter(IMemoryCache cache)
        {
            _retryAfterDuration = TimeSpan.FromSeconds(5);
            _cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // FIXME: we do not want any allocations here, and could use reflection to pull PrivateAddress/_numbers
            var hash = context.HttpContext.Connection.RemoteIpAddress?.GetAddressBytes();
            var cacheKey = WyHash64.ComputeHash64(hash, Seed);

            var now = DateTimeOffset.UtcNow;
            var retryAfter = now.Add(_retryAfterDuration);
            if (_cache.Get<int>(cacheKey) == 0)
            {
                _cache.Set(cacheKey, 1, retryAfter);
                await next();
            }
            else
            {
                TooManyRequests(context, retryAfter);
            }
        }

        private void TooManyRequests(ActionContext context, DateTimeOffset retryAfter)
        {
            context.HttpContext.Response.StatusCode = (int) HttpStatusCode.TooManyRequests;
            context.HttpContext.Response.Headers.TryAdd(HeaderNames.RetryAfter, retryAfter.ToString("r"));
            context.HttpContext.Response.Headers.TryAdd(HeaderNames.RetryAfter, _retryAfterDuration.Seconds.ToString());
        }
    }
}