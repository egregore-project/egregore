// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore.Logging
{
    public class ColorConsoleLogger : ILogger
    {
        private readonly string _category;
        private readonly IOptions<ColorConsoleLoggerOptions> _config;

        public ColorConsoleLogger(string category, IOptions<ColorConsoleLoggerOptions> config)
        {
            _category = category;
            _config = config;
        }

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var alias = GetLogLevelAlias(logLevel);
            var previous = Console.ForegroundColor;
            var next = GetTargetColor(logLevel);

            lock (this)
            {
                Console.ForegroundColor = next;
                Console.Write($@"{alias}: ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($@"{_category}[{eventId.Id}]");
                
                Console.Write(@"      "); // alignment
                var message = formatter(state, exception);
                Console.WriteLine(message);

                Console.ForegroundColor = previous;
            }
        }

        private object GetLogLevelAlias(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "erro",
                LogLevel.Critical => "crit",
                LogLevel.None => "none",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };
        }

        private static ConsoleColor GetTargetColor(LogLevel logLevel)
        {
            var target = logLevel switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.Cyan,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.DarkRed,
                LogLevel.Critical => ConsoleColor.Red,
                LogLevel.None => ConsoleColor.DarkCyan,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };
            return target;
        }
    }
}