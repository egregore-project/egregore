// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using egregore.Configuration;
using egregore.IO;
using egregore.Network;
using egregore.Ontology;
using egregore.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        internal static unsafe IHostBuilder CreateHostBuilder(int? port, string eggPath, IKeyCapture capture,
            params string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            
            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;

                webBuilder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder
                        .AddEnvironmentVariables();
                });
                webBuilder.ConfigureKestrel((context, options) =>
                {
                    options.AddServerHeader = false;
                    var x509 = CertificateBuilder.GetOrCreateSelfSignedCert(Console.Out);
                    options.ListenLocalhost(port.GetValueOrDefault(5001), x =>
                    {
                        x.Protocols = HttpProtocols.Http2;
                        x.UseHttps(a =>
                        {
                            a.SslProtocols = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SslProtocols.Tls12 : SslProtocols.Tls13;
                            a.ServerCertificate = x509;
                        });
                    });
                });
                webBuilder.ConfigureLogging((context, loggingBuilder) => { });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddCors(o => o.AddDefaultPolicy(b =>
                    {
                        b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                        b.DisallowCredentials();
                    }));

                    var keyFileService = new ServerKeyFileService();
                    services.AddSingleton<IKeyFileService>(keyFileService);

                    capture ??= new ServerConsoleKeyCapture();
                    services.AddSingleton(capture);

                    if (capture is IPersistedKeyCapture persisted)
                        services.AddSingleton(persisted);

                    var publicKey = Crypto.SigningPublicKeyFromSigningKey(keyFileService, capture);
                    capture.Reset();
                    
                    var fingerprint = new byte[8];
                    var appString = $"{context.HostingEnvironment.ApplicationName}:" +
                                    $"{context.HostingEnvironment.EnvironmentName}:" +
                                    $"{webBuilder.GetSetting("https_port")}";
                    var app = Encoding.UTF8.GetBytes(appString);

                    fixed(byte* pk = publicKey)
                    fixed(byte* id = fingerprint)
                    fixed(byte* key = app)
                    {
                        if (NativeMethods.crypto_generichash(id, fingerprint.Length, pk, Crypto.PublicKeyBytes, key, app.Length) != 0)
                            throw new InvalidOperationException(nameof(NativeMethods.crypto_generichash));
                    }

                    var serverId = Crypto.ToHexString(fingerprint);
                    services.Configure<WebServerOptions>(context.Configuration.GetSection("WebServer"));
                    services.Configure<WebServerOptions>(o =>
                    {
                        o.PublicKey = publicKey;
                        o.ServerId = serverId;
                        o.EggPath = eggPath;
                    });

                    services.AddAntiforgery(options =>
                    {
                        options.Cookie.Name = $"_{serverId}_xsrf";
                        options.Cookie.HttpOnly = true;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                        options.HeaderName = "X-XSRF-Token";
                    });

                    services.AddSingleton<PeerBus>();
                });
                webBuilder.Configure((context, appBuilder) => { appBuilder.UseCors(); });
                webBuilder.UseStartup<WebServer>();
            });

            return builder;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache(o => { });
            services.AddSingleton<ThrottleFilter>();
            services.AddControllersWithViews();
            services.AddRouting(o =>
            {
                o.AppendTrailingSlash = true;
                o.LowercaseUrls = true;
                o.LowercaseQueryStrings = false;
            });
            services.AddHostedService<WebServerStartup>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // manually instanced singletons are not cleaned up by DI on application exit
            app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
                app.ApplicationServices.GetService<IPersistedKeyCapture>()?.Dispose());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseStatusCodePagesWithReExecute("/error/{0}");
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseSecurityHeaders();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}