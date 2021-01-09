// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using egregore.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using System;

namespace egregore.Web.Controllers
{
    public class WebMetaController : Controller
    {
        [DailyCache]
        [StaticFileRoute]
        [HttpGet("robots.txt")]
        public ContentResult RobotsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("user-agent: *");
            sb.AppendLine("disallow: /");
            return Content(sb.ToString(), Constants.MediaTypeNames.Text.Plain, Encoding.UTF8);
        }

        [YearlyCache]
        [StaticFileRoute]
        [HttpGet("/.well-known/dnt-policy.txt")]
        public async Task<IActionResult> DoNotTrackAsync()
        {
            var assembly = typeof(WebMetaController).Assembly;
            var resource = assembly.GetManifestResourceStream("egregore.Web.policy.dnt-policy-1.0.txt");
            if (resource == default)
                return NotFound();

            var sb = new StringBuilder();
            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            while (true)
            {
                var read = await resource.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (read == 0)
                    break;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
            }
            ArrayPool<byte>.Shared.Return(buffer);

            return Content(sb.ToString(), Constants.MediaTypeNames.Text.Plain, Encoding.UTF8);
        }
    }
}