// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using egregore.Configuration;
using egregore.IO;
using egregore.Ontology;
using egregore.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

        public static void Run(string eggPath, IKeyCapture capture, params string[] args)
        {
            PrintMasthead();

            var builder = CreateHostBuilder(eggPath, capture, args);
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

        internal static unsafe IHostBuilder CreateHostBuilder(string eggPath, IKeyCapture capture, params string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            
            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                Activity.DefaultIdFormat = ActivityIdFormat.W3C;

                webBuilder.ConfigureKestrel((context, options) =>
                {
                    options.AddServerHeader = false;
                });
                webBuilder.ConfigureLogging((context, loggingBuilder) => { });
                webBuilder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddEnvironmentVariables();
                });
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

                    var publicKey = new byte[Crypto.PublicKeyBytes];

                    try
                    {
                        capture.Reset();
                        var sk = Crypto.LoadSecretKeyPointerFromFileStream(keyFileService.GetKeyFilePath(), keyFileService.GetKeyFileStream(), capture);
                        Crypto.PublicKeyFromSecretKey(sk, publicKey);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError(e.ToString());
                        Environment.Exit(-1);
                    }

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

                    services.Configure<WebServerOptions>(o =>
                    {
                        o.PublicKey = publicKey;
                        o.ServerId = serverId;
                        o.EggPath = eggPath;
                    });
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
                app.ApplicationServices.GetRequiredService<IPersistedKeyCapture>().Dispose());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }
            
            app.UseStatusCodePagesWithReExecute("/error/{0}");
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