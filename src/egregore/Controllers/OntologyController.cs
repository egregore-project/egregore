// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using egregore.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace egregore.Controllers
{
    public class OntologyController : Controller
    {
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public OntologyController(IOptionsSnapshot<WebServerOptions> options)
        {
            _options = options;
        }

        [HttpGet("whois")]
        public IActionResult WhoIs()
        {
            return Ok(new
            {
                PublicKey = Crypto.ToHexString(_options.Value.PublicKey)
            });
        }
    }
}