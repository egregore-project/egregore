// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace egregore.Extensions
{
    public static class UrlHelperExtensions
    {
        public static StringHtmlContent BaseUrl(this IUrlHelper urlHelper)
        {
            var request = urlHelper.ActionContext.HttpContext.Request;
            return new StringHtmlContent($"{request.Scheme}://{request.Host.Value}{request.PathBase.Value}");
        }
    }
}