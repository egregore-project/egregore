// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;

namespace egregore.Cryptography
{
    internal static class ConsoleExtensions
    {
        public static void WriteInfoLine(this TextWriter writer, string value, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            writer.WriteLine(value, args);
            Console.ResetColor();
        }

        public static void WriteInfo(this TextWriter writer, string value, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            writer.Write(value, args);
            Console.ResetColor();
        }

        public static void WriteWarningLine(this TextWriter writer, string value, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            writer.WriteLine(value, args);
            Console.ResetColor();
        }

        public static void WriteErrorLine(this TextWriter writer, string value, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            writer.WriteLine(value, args);
            Console.ResetColor();
        }
    }
}