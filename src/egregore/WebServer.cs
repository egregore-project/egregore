// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.IO;
using egregore.Ontology;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal sealed class WebServer
    {
        public static void Run(string eggPath, params string[] args)
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
                        if (context.HostingEnvironment.IsDevelopment())
                        {
                            StaticWebAssetsLoader.UseStaticWebAssets(context.HostingEnvironment, context.Configuration);
                        }
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

                        var capture = new ServerConsoleKeyCapture();
                        services.AddSingleton<IKeyCapture>(capture);
                        services.AddSingleton<IPersistedKeyCapture>(capture);

                        var publicKey = new byte[Crypto.PublicKeyBytes];

                        unsafe
                        {
                            if (!KeyFileManager.TryLoadKeyFile(keyFileService.GetKeyFileStream(), Console.Out, Console.Error, out var _, capture))
                                Environment.Exit(-1);

                            var sk = Crypto.LoadSecretKeyPointerFromFileStream(keyFileService.GetKeyFilePath(),
                                keyFileService.GetKeyFileStream(), capture);

                            Crypto.PublicKeyFromSecretKey(sk, publicKey);
                        }

                        services.Configure<WebServerOptions>(o =>
                        {
                            o.PublicKey = publicKey;
                            o.EggPath = eggPath;
                        });
                    });
                    webBuilder.Configure((context, appBuilder) =>
                    {
                        appBuilder.UseCors();
                    });
                    webBuilder.UseStartup<WebServer>();
                });

            var host = builder.Build();

            host.Run();
        }

        public WebServer(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHostedService<ServerStartup>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // self-created singletons are not cleaned up by DI on application exit
            app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
                app.ApplicationServices.GetRequiredService<IPersistedKeyCapture>().Dispose());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        #region Startup

        internal sealed class ServerStartup : IHostedService
        {
            private readonly IOptionsMonitor<WebServerOptions> _options;
            private readonly ILogger<ServerStartup> _logger;
            private OntologyLog _ontology;

            public ServerStartup(IOptionsMonitor<WebServerOptions> options, ILogger<ServerStartup> logger)
            {
                _options = options;
                _logger = logger;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                _logger?.LogInformation("Restoring ontology logs started");
                var store = new LogStore(_options.CurrentValue.EggPath);
                store.Init();

                var owner = _options.CurrentValue.PublicKey;
                _ontology = new OntologyLog(owner);
                _logger?.LogInformation("Restoring ontology logs completed");
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}