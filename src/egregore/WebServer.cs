// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Ontology;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal sealed class WebServer
    {
        public static void Run(string[] args)
        {
            var keyFilePath = Constants.DefaultKeyFilePath;
            if (!File.Exists(keyFilePath))
            {
                Console.Error.WriteLine("Cannot start server without a key file");
                return;
            }

            var eggPath = Constants.DefaultEggPath;
            if (!File.Exists(eggPath))
            {
                Console.Error.WriteLine("Cannot start server without an egg");
                return;
            }

            #region Masthead 

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
            #endregion

            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices((context, services) =>
                    {
                        var pk= Crypto.PublicKeyFromSecretKey(new ProgramKeyFileService());

                        services.Configure<WebServerOptions>(o =>
                        {
                            o.PublicKey = pk;
                            o.EggPath = eggPath;
                        });
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
            services.AddSingleton<ProgramKeyFileService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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