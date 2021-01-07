// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using egregore.Cryptography;
using egregore.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace egregore
{
    public sealed class WebServer
    {
        public WebServer(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static void Run(int? port, string eggPath, IKeyCapture capture, params string[] args)
        {
            PrintMasthead();
            var builder = CreateHostBuilder(port, eggPath, capture, args);
            var host = builder.Build();
            host.Run();
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
                    options.AddServerHeader = false;
                    var x509 = CertificateBuilder.GetOrCreateSelfSignedCert(Console.Out);
                    options.ListenLocalhost(port.GetValueOrDefault(5001), x =>
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
                    
                    //loggingBuilder.AddLightning(() =>
                    //{
                    //    throw new NotImplementedException("configuring WebServerOptions needs to be higher");
                    //    // return Path.Combine(Constants.DefaultRootPath, $"{options.Value.PublicKeyString}_logs.egg");
                    //});

                    if (context.HostingEnvironment.IsDevelopment()) // unnecessary overhead
                        loggingBuilder.AddColorConsole();
                });

                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddWebServer(eggPath, capture, context.HostingEnvironment, context.Configuration, webBuilder);
                });

                webBuilder.Configure((context, app) =>
                {
                    app.UseWebServer(context.HostingEnvironment);
                });

                webBuilder.UseStaticWebAssets();

                //var previousWebRoot = webBuilder.GetSetting(WebHostDefaults.WebRootKey);
                //var previousContentRoot = webBuilder.GetSetting(WebHostDefaults.ContentRootKey);
                
                var contentRoot = Directory.GetCurrentDirectory();
                var webRoot = Path.Combine(contentRoot, "wwwroot");
                if (!File.Exists(Path.Combine(webRoot, "css", "signin.css")))
                    webRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", "egregore.Client", "wwwroot"));

                webBuilder.UseContentRoot(contentRoot);
                webBuilder.UseWebRoot(webRoot);
            });

            return builder;
        }
    }
}