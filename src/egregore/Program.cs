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
        private static int _port;

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
                if(ProcessCommandLineArguments(arguments))
                    NonInteractiveStartup(_port, args, arguments);
            }
            finally
            {
                keyFileStream?.Dispose();
            }
        }

        private static void NonInteractiveStartup(int? port, string[] args, Queue<string> arguments)
        {
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
                RunAsServer(port, arguments, new PlaintextKeyCapture(password, password), false);
            }
        }

        private static bool ProcessCommandLineArguments(Queue<string> arguments)
        {
            while (arguments.Count > 0)
            {
                var arg = arguments.Dequeue();
                switch (arg.ToLower())
                {
                    case "--nolock":
                        _exclusiveLock = false;
                        break;
                    case "--cert":
                    case "--certs":
                    case "-c":
                    {
                        var freshOrClear = arguments.EndOfSubArguments() ? "false" : arguments.Dequeue();
                        if(freshOrClear?.Equals("clear", StringComparison.OrdinalIgnoreCase) ?? false)
                            CertificateBuilder.ClearAll(Console.Out);
                        else
                            CertificateBuilder.GetOrCreateSelfSignedCert(Console.Out, freshOrClear?.Equals("fresh", StringComparison.OrdinalIgnoreCase) ?? false);
                        return false;
                    }
                    case "--port":
                    case "-p":
                    {
                        var portString = arguments.Dequeue();
                        int.TryParse(portString, out _port);
                        break;
                    }
                    case "--server":
                    case "-s":
                    {
                        RunAsServer(null, arguments, null, true);
                        return false;
                    }
                    case "--keygen":
                    case "-k":
                    {
                        var keyPath = arguments.EndOfSubArguments() ? Constants.DefaultKeyFilePath : arguments.Dequeue();
                        KeyFileManager.Create(keyPath, true, false, Constants.ConsoleKeyCapture);
                        return false;
                    }
                    case "--egg":
                    case "-e":
                    {
                        var eggPath = arguments.EndOfSubArguments() ? Constants.DefaultEggPath : arguments.Dequeue();
                        EggFileManager.Create(eggPath);
                        return false;
                    }
                    case "--append":
                    case "-a":
                        Append(arguments);
                        return false;
                }
            }

            return true;
        }

        private static bool _exclusiveLock = true;

        private static void RunAsServer(int? port, Queue<string> arguments, IKeyCapture capture, bool interactive)
        {
            var keyPath = arguments.EndOfSubArguments() ? Constants.DefaultKeyFilePath : arguments.Dequeue();

            if (!KeyFileManager.TryResolveKeyPath(keyPath, out keyFilePath, false, true))
                return;

            var shouldCreateKeyFile = !File.Exists(keyFilePath) || new FileInfo(keyFilePath).Length == 0;
            if (shouldCreateKeyFile)
                File.WriteAllBytes(keyFilePath, new byte[KeyFileManager.KeyFileBytes]);

            Console.Out.WriteInfoLine($"Key file path resolved to '{keyFilePath}'");

            if (shouldCreateKeyFile && !KeyFileManager.Create(keyFilePath, false, true, capture ?? Constants.ConsoleKeyCapture))
            {
                Console.Error.WriteErrorLine("Cannot start server without a key file");
                return;
            }

            if (_exclusiveLock)
            {
                try
                {
                    keyFileStream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch (IOException)
                {
                    Console.Error.WriteErrorLine("Could not obtain exclusive lock on key file");
                    return;
                }
            }
            else
            {
                try
                {
                    keyFileStream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (IOException)
                {
                    Console.Error.WriteErrorLine("Could not open key file");
                    return;
                }
            }
            
            var eggPath = Environment.GetEnvironmentVariable(Constants.EnvVars.EggFilePath);
            if (string.IsNullOrWhiteSpace(eggPath))
                eggPath = Constants.DefaultEggPath;

            Console.Out.WriteInfoLine($"Egg file path resolved to '{eggPath}'");

            if (!File.Exists(eggPath) && !EggFileManager.Create(eggPath))
                Console.Error.WriteWarningLine("Server started without an egg");

            capture?.Reset();

            if (!interactive)
            {
                LaunchBrowserUrl($"https://localhost:{port.GetValueOrDefault(5001)}");
            }

            WebServer.Run(port, eggPath, capture, arguments.ToArray());
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
                case  Constants.Commands.GrantRole:
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

        public static void LaunchBrowserUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }

}