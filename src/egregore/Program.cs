// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace egregore
{
    public static class Program
    {
        public static void Main(params string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if(!Debugger.IsAttached)
                    Environment.Exit(Marshal.GetHRForException((Exception) e.ExceptionObject));
            };

            var arguments = new Queue<string>(args);
            while (arguments.Count > 0)
            {
                var arg = arguments.Dequeue();
                switch (arg.ToLower())
                {
                    case "--keygen":
                    case "-k":
                        var (_, sk) = Crypto.GenerateKeyPair();
                        Console.WriteLine(WifFormatter.Serialize(sk));
                        break;
                }
            }
        }

        private static bool EndOfSubArguments(Queue<string> arguments) => arguments.Count == 0 || arguments.Peek().StartsWith("--");
    }
}