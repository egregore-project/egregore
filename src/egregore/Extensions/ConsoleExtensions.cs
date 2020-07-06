// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace egregore.Extensions
{
    internal static class ConsoleExtensions
    {
        public static void WriteErrorLine(this TextWriter writer, string value, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            writer.WriteLine(value, args);
            Console.ResetColor();
        }

        public static void WriteWarningLine(this TextWriter writer, string value, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            writer.WriteLine(value, args);
            Console.ResetColor();
        }
    }
}