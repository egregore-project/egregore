// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using egregore.Configuration;
using egregore.IO;
using egregore.Ontology;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace egregore
{
    internal sealed class WebServer
    {
        public WebServer(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public static void Run(string eggPath, IKeyCapture capture, params string[] args)
        {
            Console.ResetColor();

            #region Masthead

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

            #endregion

            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    Activity.DefaultIdFormat = ActivityIdFormat.W3C;

                    webBuilder.ConfigureKestrel((context, options) => { });
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

                        unsafe
                        {
                            try
                            {
                                capture.Reset();
                                var sk = Crypto.LoadSecretKeyPointerFromFileStream(keyFileService.GetKeyFilePath(),
                                    keyFileService.GetKeyFileStream(), capture);
                                Crypto.PublicKeyFromSecretKey(sk, publicKey);
                            }
                            catch (Exception e)
                            {
                                Trace.TraceError(e.ToString());
                                Environment.Exit(-1);
                            }
                        }

                        services.Configure<WebServerOptions>(o =>
                        {
                            o.PublicKey = publicKey;
                            o.EggPath = eggPath;
                        });
                    });
                    webBuilder.Configure((context, appBuilder) => { appBuilder.UseCors(); });
                    webBuilder.UseStartup<WebServer>();
                });

            var host = builder.Build();

            host.Run();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
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

            app.UseHttpsRedirection();
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