// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using egregore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore.Logging
{
    public sealed class LightningLoggerProvider : ILoggerProvider
    {
        private readonly LightningLoggingStore _store;
        private readonly IOptions<WebServerOptions> _options;
        private readonly ConcurrentDictionary<string, LightningLogger> _loggers = new ConcurrentDictionary<string, LightningLogger>();

        public LightningLoggerProvider(LightningLoggingStore store, IOptions<WebServerOptions> options)
        {
            _store = store;
            _options = options;
        }

        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new LightningLogger(_store, _options));
        public void Dispose() => _loggers.Clear();
    }
}