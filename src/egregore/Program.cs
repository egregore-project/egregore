// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using egregore.Extensions;
using egregore.IO;
using egregore.Ontology;

namespace egregore
{
    internal static class Program
    {
        internal static string keyFilePath;
        internal static FileStream keyFileStream;

        [ExcludeFromCodeCoverage]
        public static void Main(params string[] args)
        {
            // Pre-initialize libsodium as deferring causes
            // assertion errors on Linux containers
            Crypto.Initialize();

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (!Debugger.IsAttached)
                        Environment.Exit(Marshal.GetHRForException((Exception) e.ExceptionObject));
                };

                var arguments = new Queue<string>(args);
                switch (arguments.Count)
                {
                    case 0:
                        Console.Out.WriteInfoLine("Starting server in non-interactive mode.");
                        var password = Environment.GetEnvironmentVariable(Constants.EnvVars.KeyFilePassword);
                        if (string.IsNullOrWhiteSpace(password))
                        {
                            Console.Error.WriteErrorLine($"Could not locate '{Constants.EnvVars.KeyFilePassword}' variable for container deployment.");
                            Console.Out.WriteInfoLine("To run the server interactively to input a password, use the --server argument.");
                            Environment.Exit(-1);
                        }
                        else
                        {
                            RunAsServer(args, arguments, new PlaintextKeyCapture(password, password));
                        }

                        break;
                    default:
                        ProcessCommandLineArguments(args, arguments);
                        break;
                }
            }
            finally
            {
                keyFileStream?.Dispose();
            }
        }

        private static void ProcessCommandLineArguments(string[] args, Queue<string> arguments)
        {
            while (arguments.Count > 0)
            {
                var arg = arguments.Dequeue();
                switch (arg.ToLower())
                {
                    case "--server":
                    case "-s":
                    {
                        if (!RunAsServer(args, arguments, null))
                            return;
                        break;
                    }
                    case "--keygen":
                    case "-k":
                    {
                        var keyPath = arguments.EndOfSubArguments() ? Constants.DefaultKeyFilePath : arguments.Dequeue();
                        if (KeyFileManager.Create(keyPath, true, false, Constants.ConsoleKeyCapture))
                            return;
                        break;
                    }
                    case "--egg":
                    case "-e":
                    {
                        var eggPath = arguments.EndOfSubArguments() ? Constants.DefaultEggPath : arguments.Dequeue();
                        EggFileManager.Create(eggPath);
                        break;
                    }
                    case "--append":
                    case "-a":
                        Append(arguments);
                        break;
                }
            }
        }

        private static bool RunAsServer(string[] args, Queue<string> arguments, IKeyCapture capture)
        {
            var keyPath = arguments.EndOfSubArguments() ? Constants.DefaultKeyFilePath : arguments.Dequeue();

            if (!KeyFileManager.TryResolveKeyPath(keyPath, out keyFilePath, false, true))
                return false;

            if (!File.Exists(keyFilePath) || new FileInfo(keyFilePath).Length == 0)
                File.WriteAllBytes(keyFilePath, new byte[KeyFileManager.KeyFileBytes]);

            Console.Out.WriteInfoLine($"Key file path resolved to '{keyFilePath}'");

            if (!KeyFileManager.Create(keyFilePath, false, true, capture ?? Constants.ConsoleKeyCapture))
            {
                Console.Error.WriteErrorLine("Cannot start server without a key file");
                return false;
            }

            try
            {
                keyFileStream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                Console.Error.WriteErrorLine("Could not obtain exclusive lock on key file");
                return false;
            }

            var eggPath = Environment.GetEnvironmentVariable(Constants.EnvVars.EggFilePath);
            if (string.IsNullOrWhiteSpace(eggPath))
                eggPath = Constants.DefaultEggPath;

            Console.Out.WriteInfoLine($"Egg file path resolved to '{eggPath}'");

            if (!File.Exists(eggPath) && !EggFileManager.Create(eggPath))
                Console.Error.WriteWarningLine("Server started without an egg");

            capture?.Reset();
            WebServer.Run(eggPath, capture, args);
            return true;
        }

        private static void Append(Queue<string> arguments)
        {
            if (arguments.EndOfSubArguments())
            {
                Console.Error.WriteErrorLine("Missing append command");
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
            if (arguments.EndOfSubArguments())
            {
                Console.Error.WriteErrorLine("Missing role data");
                return;
            }

            var value = arguments.Dequeue();
            if (arguments.EndOfSubArguments())
            {
                Console.Error.WriteErrorLine("Missing privilege");
                return;
            }

            var authorityString = arguments.Dequeue();
            var authority = authorityString.ToBinary();

            if (arguments.EndOfSubArguments())
            {
                Console.Error.WriteErrorLine("Missing subject");
                return;
            }

            var subjectString = arguments.Dequeue();
            var subject = subjectString.ToBinary();

            if (arguments.EndOfSubArguments())
            {
                Console.Error.WriteErrorLine("Missing signature");
                return;
            }

            var signatureString = arguments.Dequeue();
            var signature = signatureString.ToBinary();

            var grant = new GrantRole(value, authority, subject, signature);
            if (!grant.Verify()) Console.Error.WriteErrorLine("Invalid signature");

            Console.WriteLine("TODO: append privilege to ontology");
        }
    }
}