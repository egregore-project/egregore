// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using egregore.Extensions;
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
                            if (!TryResolveKeyPath(arguments, out keyFilePath, false))
                                return;

                            try
                            {
                                keyFileStream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                            }
                            catch (IOException)
                            {
                                Console.Error.WriteErrorLine("Could not obtain exclusive lock on key file");
                                break;
                            }
                            
                            WebServer.Run(args);
                            break;
                        }
                        case "--keygen":
                        case "-k":
                        {
                            if (!TryResolveKeyPath(arguments, out keyFilePath, true))
                                return;
                            keyFileStream = new FileStream(keyFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                            if (!PasswordStorage.TryGenerateKeyFile(keyFileStream, Console.Out, Console.Error, Constants.ConsoleKeyCapture))
                                return;
                            Console.Out.WriteLine(Strings.KeyFileSuccess);
                            break;
                        }
                        case "--egg":
                        case "-e":
                            var eggPath = arguments.EndOfSubArguments() ? Constants.DefaultEggPath : arguments.Dequeue();
                            CreateEgg(eggPath);
                            break;
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

        public static bool TryResolveKeyPath(Queue<string> arguments, out string fullKeyPath, bool warnIfExists)
        {
            fullKeyPath = default;
            var keyPath = arguments.EndOfSubArguments() ? Constants.DefaultKeyFilePath : arguments.Dequeue();
            
            Directory.CreateDirectory(".egregore");
            if (keyPath != Constants.DefaultKeyFilePath && keyPath.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                Console.Error.WriteErrorLine(Strings.InvalidCharactersInPath);
                return false;
            }

            try
            {
                fullKeyPath = Path.GetFullPath(keyPath);
                if (!Path.HasExtension(fullKeyPath))
                    fullKeyPath = Path.ChangeExtension(keyPath, ".key");
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine(Strings.InvalidKeyFilePath);
                return false;
            }

            if (warnIfExists && File.Exists(fullKeyPath))
            {
                Console.Error.WriteWarningLine(Strings.KeyFileAlreadyExists);
            }
            else if (!warnIfExists && !File.Exists(fullKeyPath))
            {
                Console.Error.WriteErrorLine(Strings.KeyFileIsMissing);
                return false;
            }

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
            if (!grant.Verify())
            {
                Console.Error.WriteErrorLine("Invalid signature");
            }

            Console.WriteLine("TODO: append privilege to ontology");
        }

        private static void CreateEgg(string eggPath)
        {
            if (eggPath.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                Console.Error.WriteErrorLine(Strings.InvalidCharactersInPath);
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
                Console.Error.WriteErrorLine("Invalid path");
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
                Console.Error.WriteErrorLine("Failed to create egg file at '{0}'", eggPath);
            }
        }
    }
}