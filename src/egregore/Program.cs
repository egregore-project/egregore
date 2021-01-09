// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using egregore.Configuration;
using egregore.Cryptography;
using egregore.Extensions;
using egregore.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal static class Program
    {
        internal static string keyFilePath;
        internal static FileStream keyFileStream;
        private static int _port;

        private static bool _exclusiveLock = true;

        [ExcludeFromCodeCoverage]
        public static void Main(params string[] args)
        {
            Crypto.Initialize(); // Pre-initialize libsodium as deferring causes assertion errors on Linux containers

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (!Debugger.IsAttached)
                        Environment.Exit(Marshal.GetHRForException((Exception) e.ExceptionObject));
                };

                var arguments = new Queue<string>(args);

                if (ProcessCommandLineArguments(arguments))
                    NonInteractiveStartup(_port, arguments);
            }
            finally
            {
                keyFileStream?.Dispose();
            }
        }

        private static void NonInteractiveStartup(int? port, Queue<string> arguments)
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
                    #region Unattended Commands

                    case "--nolock":
                        _exclusiveLock = false;
                        break;

                    case "--port":
                    case "-p":
                    {
                        var portString = arguments.Dequeue();
                        int.TryParse(portString, out _port);
                        break;
                    }

                    #endregion

                    #region CLI Commands

                    case "--cert":
                    case "--certs":
                    case "-c":
                    {
                        var freshOrClear = arguments.EndOfSubArguments() ? "false" : arguments.Dequeue();
                        if (freshOrClear?.Equals("clear", StringComparison.OrdinalIgnoreCase) ?? false)
                            CertificateBuilder.ClearAll(Console.Out);
                        else
                            CertificateBuilder.GetOrCreateSelfSignedCert(Console.Out,
                                freshOrClear?.Equals("fresh", StringComparison.OrdinalIgnoreCase) ?? false);
                        return false;
                    }

                    case "--egg":
                    case "-e":
                    {
                        var eggPath = arguments.EndOfSubArguments() ? Constants.DefaultEggPath : arguments.Dequeue();
                        EggFileManager.Create(eggPath);
                        return false;
                    }

                    case "--keygen":
                    case "-k":
                    {
                        var keyPath = arguments.EndOfSubArguments()
                            ? Constants.DefaultKeyFilePath
                            : arguments.Dequeue();
                        KeyFileManager.Create(keyPath, true, false, Constants.ConsoleKeyCapture);
                        return false;
                    }

                    case "--server":
                    case "-s":
                    {
                        RunAsServer(null, arguments, null, true);
                        return false;
                    }

                    #endregion
                }
            }

            return true;
        }

        private static void RunAsServer(int? port, Queue<string> arguments, IKeyCapture capture, bool interactive)
        {
            var keyPath = arguments.EndOfSubArguments() ? Constants.DefaultKeyFilePath : arguments.Dequeue();

            if (!KeyFileManager.TryResolveKeyPath(keyPath, out keyFilePath, false, true))
                return;

            var shouldCreateKeyFile = !File.Exists(keyFilePath) || new FileInfo(keyFilePath).Length == 0;
            if (shouldCreateKeyFile)
                File.WriteAllBytes(keyFilePath, new byte[KeyFileManager.KeyFileBytes]);

            Console.Out.WriteInfoLine($"Key file path resolved to '{keyFilePath}'");

            if (shouldCreateKeyFile &&
                !KeyFileManager.Create(keyFilePath, false, true, capture ?? Constants.ConsoleKeyCapture))
            {
                Console.Error.WriteErrorLine("Cannot start server without a key file");
                return;
            }

            if (_exclusiveLock)
                try
                {
                    keyFileStream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                catch (IOException)
                {
                    Console.Error.WriteErrorLine("Could not obtain exclusive lock on key file");
                    return;
                }
            else
                try
                {
                    keyFileStream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (IOException)
                {
                    Console.Error.WriteErrorLine("Could not open key file");
                    return;
                }

            var eggPath = Environment.GetEnvironmentVariable(Constants.EnvVars.EggFilePath);
            if (string.IsNullOrWhiteSpace(eggPath))
                eggPath = Constants.DefaultEggPath;

            Console.Out.WriteInfoLine($"Egg file path resolved to '{eggPath}'");

            if (!File.Exists(eggPath) && !EggFileManager.Create(eggPath))
                Console.Error.WriteWarningLine("Server started without an egg");

            capture?.Reset();

            if (!interactive)
                LaunchBrowserUrl($"https://localhost:{port.GetValueOrDefault(Constants.DefaultPort)}");

            PrintMasthead();
            var builder = CreateHostBuilder(port, eggPath, capture, arguments.ToArray());
            var host = builder.Build();
            host.Run();
        }
        
        internal static IHostBuilder CreateHostBuilder(int? port, string eggPath, IKeyCapture capture, params string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;

                webBuilder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddEnvironmentVariables();
                });

                webBuilder.ConfigureKestrel((context, options) =>
                {
                    var x509 = CertificateBuilder.GetOrCreateSelfSignedCert(Console.Out);

                    options.AddServerHeader = false;
                    options.ListenLocalhost(port.GetValueOrDefault(Constants.DefaultPort), x =>
                    {
                        x.Protocols = HttpProtocols.Http1AndHttp2;
                        x.UseHttps(a =>
                        {
                            a.SslProtocols = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                ? SslProtocols.Tls12
                                : SslProtocols.Tls13;
                            a.ServerCertificate = x509;
                        });
                    });
                });

                webBuilder.ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();

                    loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                    loggingBuilder.AddDebug();
                    loggingBuilder.AddEventSourceLogger();
                    loggingBuilder.AddLogging(() =>
                    {
                        var serviceProvider = loggingBuilder.Services.BuildServiceProvider();
                        return Path.Combine(Constants.DefaultRootPath, $"{serviceProvider.GetRequiredService<IOptions<WebServerOptions>>().Value.PublicKeyString}_logs.egg");
                    });

                    if (context.HostingEnvironment.IsDevelopment()) 
                        loggingBuilder.AddColorConsole(); // unnecessary overhead
                });

                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddWebServer(eggPath, port.GetValueOrDefault(Constants.DefaultPort), capture, context.HostingEnvironment, context.Configuration);
                });

                webBuilder.UseStartup<Startup>();
                
                var contentRoot = Directory.GetCurrentDirectory();
                var webRoot = Path.Combine(contentRoot, "wwwroot");
                if (!File.Exists(Path.Combine(webRoot, "css", "signin.css")))
                    webRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", "egregore.Client", "wwwroot"));
                webBuilder.UseContentRoot(contentRoot);
                webBuilder.UseWebRoot(webRoot);
            });

            return builder;
        }

        public static void LaunchBrowserUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) {UseShellExecute = true});
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
        }
        
        private static void PrintMasthead()
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(@"                                        
                       @@@@             
                  @   @@@@@@            
                  @@@   @@    @@@@      
                   .@@@@@@@@@@@@@@@     
        @@@@@@@@@@@@  @@@@@@@@@         
       @@@@@@@@@@@@@@      %@@@@&       
        .@/       @@@   .@@@@@@@@@@@    
           @@  @@@@@   @@@@@@     %@@   
   @       @@@@@@@    @@@@@@       @@@  
   @@      @@@@@@@@@@@@@@@         @@@  
   @@              @@@  @         @@@@  
    @@@            @@@@ @@@@@@@@@@@@@   
    .@@@@@      @@@@@@/  @@@@@@@@@@@    
      @@@@@@@@@@@@@@@      @@@@@@       
         @@@@@@@@@@                     
");
            Console.ResetColor();
        }

    }
}