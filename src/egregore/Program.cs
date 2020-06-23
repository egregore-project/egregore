// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using egregore.Ontology;

namespace egregore
{
    
    internal static class Program
    {
        [ExcludeFromCodeCoverage]
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
                    case "--server":
                    case "-s":
                        WebServer.Run(args);
                        break;
                    case "--keygen":
                    case "-k":
                        var keyPath = EndOfSubArguments(arguments) ? Constants.DefaultKeyPath : arguments.Dequeue();
                        GenerateKey(keyPath, Constants.DefaultKeyPath);
                        break;
                    case "--egg":
                    case "-e":
                        var eggPath = EndOfSubArguments(arguments) ? Constants.DefaultEggPath : arguments.Dequeue();
                        CreateEgg(eggPath);
                        break;
                    case "--append":
                    case "-a":
                        Append(arguments);
                        break;
                }
            }
        }

        private static void Append(Queue<string> arguments)
        {
            if (EndOfSubArguments(arguments))
            {
                Console.Error.WriteLine("Missing append command");
                return;
            }

            var command = arguments.Dequeue();
            switch (command)
            {
                case "grantrole":
                    GrantRole(arguments);
                    break;
            }
        }

        private static void GrantRole(Queue<string> arguments)
        {
            if (EndOfSubArguments(arguments))
            {
                Console.Error.WriteLine("Missing role data");
                return;
            }

            var value = arguments.Dequeue();
            if (EndOfSubArguments(arguments))
            {
                Console.Error.WriteLine("Missing privilege");
                return;
            }

            var authorityString = arguments.Dequeue();
            var authority = authorityString.ToBinary();

            if (EndOfSubArguments(arguments))
            {
                Console.Error.WriteLine("Missing subject");
                return;
            }

            var subjectString = arguments.Dequeue();
            var subject = subjectString.ToBinary();

            if (EndOfSubArguments(arguments))
            {
                Console.Error.WriteLine("Missing signature");
                return;
            }

            var signatureString = arguments.Dequeue();
            var signature = signatureString.ToBinary();

            var grant = new GrantRole(value, authority, subject, signature);
            if (!grant.Verify())
            {
                Console.Error.WriteLine("Invalid signature");
            }

            Console.WriteLine("TODO: append privilege to ontology");
        }

        private static void GenerateKey(string keyPath, string defaultPath)
        {
            var (_, sk) = Crypto.GenerateKeyPair();
            Directory.CreateDirectory(".egregore");
            if (keyPath != defaultPath && keyPath.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                Console.Error.WriteLine("Invalid characters in path");
                return;
            }

            try
            {
                keyPath = Path.GetFullPath(keyPath);
                if (!Path.HasExtension(keyPath))
                    keyPath = Path.ChangeExtension(keyPath, ".key");
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteLine("Invalid path");
                return;
            }

            if (File.Exists(keyPath))
            {
                Console.Error.WriteLine("Key file already exists");
                return;
            }

            try
            {
                File.WriteAllText(keyPath, WifFormatter.Serialize(sk));
                Console.WriteLine("Generated new key pair '{0}'", Path.GetFileName(keyPath));
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Failed to generated key");
                throw;
            }
        }

        private static void CreateEgg(string eggPath)
        {
            if (eggPath.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                Console.Error.WriteLine("Invalid characters in path");
                return;
            }

            try
            {
                eggPath = Path.GetFullPath(eggPath);
                if (!Path.HasExtension(eggPath))
                    eggPath = Path.ChangeExtension(eggPath, ".egg");
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteLine("Invalid path");
                return;
            }

            try
            {
                var store = new LogStore(eggPath);
                store.Init();
                Console.WriteLine("Created egg file '{0}'", Path.GetFileName(eggPath));
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteLine("Failed to create egg file at '{0}'", eggPath);
            }
        }

        private static bool EndOfSubArguments(Queue<string> arguments) => arguments.Count == 0 || arguments.Peek().StartsWith("-");
    }
}