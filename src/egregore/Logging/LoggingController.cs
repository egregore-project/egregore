// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace egregore.Logging
{
    public class LoggingController : Controller
    {
        private readonly LightningLoggingStore _store;

        public LoggingController(LightningLoggingStore store)
        {
            _store = store;
        }

        private CancellationToken CancellationToken => HttpContext.RequestAborted;

        [HttpGet("api/logs")]
        public IActionResult Index()
        {
            var entries = _store.Get(CancellationToken);

            return Ok(entries);
        }
    }
}