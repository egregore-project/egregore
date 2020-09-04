// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore.Logging
{
    public class ColorConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers =
            new ConcurrentDictionary<string, ColorConsoleLogger>();

        private readonly IOptions<ColorConsoleLoggerOptions> _options;

        public ColorConsoleLoggerProvider(IOptions<ColorConsoleLoggerOptions> options)
        {
            _options = options;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(name, _options));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}