// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Diagnostics;
using System.Net;
using egregore.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace egregore.Controllers
{
    public class ErrorController : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("error/{statusCode?}")]
        public IActionResult Error(int statusCode = 500)
        {
            Response.StatusCode = statusCode;

            switch (statusCode)
            {
                case 404:
                    return NotFound();
                case (int) HttpStatusCode.TooManyRequests:
                    return View("TooManyRequests");
            }

            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();

            var model = new ErrorViewModel
            {
                StatusCode = statusCode,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier ?? "<None>",
                ErrorMessage = feature?.Error?.Message ?? "<None>"
            };

            return View(model);
        }
    }
}