// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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