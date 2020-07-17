// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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