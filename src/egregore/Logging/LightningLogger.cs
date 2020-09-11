// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using egregore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore.Logging
{
    public class LightningLogger : ILogger
    {
        private readonly LightningLoggingStore _store;

        public LightningLogger(LightningLoggingStore store, IOptions<WebServerOptions> options)
        {
            _store = store;
            _store.Init(Path.Combine(Constants.DefaultRootPath, $"{options.Value.PublicKeyString}_logs.egg"));
        }

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            _store.Append(logLevel, eventId, state, exception, formatter);
        }
    }
}