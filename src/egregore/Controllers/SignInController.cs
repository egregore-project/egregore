// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using egregore.Configuration;
using egregore.Models;
using egregore.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WyHash;

namespace egregore.Controllers
{
    public class SignInController : Controller
    {
        private static readonly ulong Seed = BitConverter.ToUInt64(Encoding.UTF8.GetBytes(nameof(SignInController)));
        private readonly IOptionsSnapshot<WebServerOptions> _options;

        public SignInController(IOptionsSnapshot<WebServerOptions> options)
        {
            _options = options;
        }

        [AllowAnonymous]
        [HttpGet("signin")]
        [ServiceFilter(typeof(ThrottleFilter), IsReusable = true)]
        public IActionResult Index()
        {
            // FIXME: avoid allocation, also reuse the value in the filter and pass as a method parameter
            var hash = BitConverter.GetBytes(WyHash64.ComputeHash64(Request.HttpContext.Connection.RemoteIpAddress.GetAddressBytes(), Seed));

            // Challenge = sha256(wyhash(IP)[8]:ServerId[16]:Nonce[24])
            var buffer = Crypto.Nonce(48);
            for (var i = 0; i < _options.Value.ServerId.Length; i++)
                buffer[i] = (byte) _options.Value.ServerId[i];
            for (var i = 8; i < hash.Length; i++)
                buffer[i] = hash[i];

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