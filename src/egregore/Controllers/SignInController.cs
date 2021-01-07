// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using egregore.Configuration;
using egregore.Cryptography;
using egregore.Filters;
using egregore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace egregore.Controllers
{
    public class SignInController : Controller
    {
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public SignInController(IOptionsSnapshot<WebServerOptions> options)
        {
            _options = options;
        }

        [AllowAnonymous]
        [RemoteAddress]
        [Throttle]
        [HttpGet("signin")]
        public IActionResult Index([FromFilter] byte[] addressHash = default)
        {
            if (addressHash == default)
                return BadRequest();

            // Challenge = sha256(wyhash(IP)[8]:ServerId[16]:Nonce[24])
            var buffer = Crypto.Nonce(48);
            for (var i = 0; i < _options.Value.ServerId.Length; i++)
                buffer[i] = (byte) _options.Value.ServerId[i];
            for (var i = 8; i < addressHash.Length; i++)
                buffer[i] = addressHash[i];

            var challenge = Crypto.ToHexString(Crypto.Sha256(buffer));

            var model = new SignInViewModel
            {
                Challenge = challenge,
                ServerId = _options.Value.ServerId
            };

            return View(model);
        }
    }
}