// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace egregore.Filters
{
    public class BaseViewModelFilter : IAsyncActionFilter
    {
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public BaseViewModelFilter(IOptionsSnapshot<WebServerOptions> options)
        {
            _options = options;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments)
                if (argument.Value is BaseViewModel model)
                    model.ServerId = _options.Value.ServerId;

            await next();
        }
    }
}