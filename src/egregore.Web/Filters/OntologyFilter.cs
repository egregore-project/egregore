// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace egregore.Web.Filters
{
    public sealed class OntologyFilter : ActionFilterAttribute
    {
        private readonly IOntologyLog _ontology;

        public OntologyFilter(IOntologyLog ontology)
        {
            _ontology = ontology;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.TryGetValue("controller", out var cv) && cv is string controller &&
                context.ActionArguments.TryGetValue("ns", out var nsv) && nsv is string ns &&
                context.ActionArguments.TryGetValue("rs", out var rsv) && rsv is ulong rs)
            {
                var schema = _ontology.GetSchema(controller, ns, rs);
                if (schema == default)
                    context.Result = new NotFoundObjectResult(new
                    {
                        Message = $"No API found for {ns.ToLowerInvariant()}.v{rs}.{controller.ToLowerInvariant()}"
                    });
            }
        }
    }
}