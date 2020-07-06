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
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (!Debugger.IsAttached)
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
                        {
                            if (!KeyFileManager.TryResolveKeyPath(arguments, out keyFilePath, false, true))
                                return;

                            if (!File.Exists(keyFilePath) && !KeyFileManager.Create(arguments, false, true))
                            {
                                Console.Error.WriteErrorLine("Cannot start server without a key file");
                                return;
                            }

                            try
                            {
                                keyFileStream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                            }
                            catch (IOException)
                            {
                                Console.Error.WriteErrorLine("Could not obtain exclusive lock on key file");
                                break;
                            }

                            var eggPath = Constants.DefaultEggPath;
                            if (!File.Exists(eggPath) && !EggFileManager.Create(eggPath))
                            {
                                Console.Error.WriteErrorLine("Cannot start server without an egg");
                                return;
                            }
                            
                            WebServer.Run(eggPath, args);
                            break;
                        }
                        case "--keygen":
                        case "-k":
                        {
                            if (KeyFileManager.Create(arguments, true, false))
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
            finally
            {
                keyFileStream?.Dispose();
            }
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
            if (!grant.Verify())
            {
                Console.Error.WriteErrorLine("Invalid signature");
            }

            Console.WriteLine("TODO: append privilege to ontology");
        }
    }
}