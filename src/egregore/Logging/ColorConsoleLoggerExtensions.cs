// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace egregore.Logging
{
    public static class ColorConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddColorConsole(this ILoggingBuilder builder,
            Action<ColorConsoleLoggerOptions> configure = default)
        {
            builder.AddConfiguration();
            builder.Services.Configure<ColorConsoleLoggerOptions>(x =>
            {
                var options = new ColorConsoleLoggerOptions();
                configure?.Invoke(options);
            });
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, ColorConsoleLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<ColorConsoleLoggerOptions, ColorConsoleLoggerProvider>(
                builder.Services);
            return builder;
        }
    }
}