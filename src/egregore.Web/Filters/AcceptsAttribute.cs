// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Net.Http.Headers;

namespace egregore.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AcceptsAttribute : Attribute, IActionConstraint
    {
        private readonly MediaTypeHeaderValue[] _supportedMediaTypes;

        public AcceptsAttribute(params string[] contentTypes)
        {
            _supportedMediaTypes = new MediaTypeHeaderValue[contentTypes.Length];
            for (var i = 0; i < contentTypes.Length; i++)
                _supportedMediaTypes[i] = MediaTypeHeaderValue.Parse(contentTypes[i]);
        }

        public bool Accept(ActionConstraintContext context)
        {
            var headers = context.RouteContext.HttpContext.Request.GetTypedHeaders();

            foreach (var accept in headers.Accept)
            foreach (var contentType in _supportedMediaTypes)
                if (accept.IsSubsetOf(contentType))
                    return true;

            return false;
        }

        public int Order => 0;
    }
}