// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace egregore.Filters
{
    public sealed class AcceptCharsetAttribute : ActionFilterAttribute
    {
        private const string EncodingParameter = "encoding";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;
            var headers = request.GetTypedHeaders();
            var charset = headers.AcceptCharset?.FirstOrDefault();
            var encoding = charset == null ? Encoding.UTF8 : Encoding.GetEncoding(charset.Value.Value);

            if (encoding != null)
                context.ActionArguments.TryAdd(EncodingParameter, encoding);

            base.OnActionExecuting(context);
        }
    }
}