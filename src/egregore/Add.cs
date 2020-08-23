using System;
using System.Text;
using egregore.Caching;
using egregore.Configuration;
using egregore.Data;
using egregore.Events;
using egregore.Filters;
using egregore.IO;
using egregore.Network;
using egregore.Ontology;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace egregore
{
    internal static class Add
    {
        public static unsafe IServiceCollection AddWebServer(this IServiceCollection services, string eggPath, IKeyCapture capture, IWebHostEnvironment env, IConfiguration config, IWebHostBuilder webBuilder)
        {
            services.AddCors(o => o.AddDefaultPolicy(b =>
            {
                b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                b.DisallowCredentials(); // credentials are invalid when origin is *
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
            var appString = $"{env.ApplicationName}:" +
                            $"{env.EnvironmentName}:" +
                            $"{webBuilder.GetSetting("https_port")}";

            var app = Encoding.UTF8.GetBytes(appString);

            fixed (byte* pk = publicKey)
            fixed (byte* id = fingerprint)
            fixed (byte* key = app)
            {
                if (NativeMethods.crypto_generichash(id, fingerprint.Length, pk, Crypto.PublicKeyBytes, key, app.Length) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_generichash));
            }

            var serverId = Crypto.ToHexString(fingerprint);

            services.Configure<WebServerOptions>(config.GetSection("WebServer"));
            services.Configure<WebServerOptions>(o =>
            {
                o.PublicKey = publicKey;
                o.ServerId = serverId;
                o.EggPath = eggPath;
            });

            var o = new WebServerOptions();
            config.Bind(o);

            services.AddAntiforgery(o =>
            {
                o.Cookie.Name = $"_{serverId}_xsrf";
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.HeaderName = "X-XSRF-Token";
            });

            services.AddSingleton<PeerBus>();

            var ontology = new MemoryOntologyLog(publicKey);
            services.AddSingleton<IOntologyLog, MemoryOntologyLog>(r => ontology);
            
            var mvc = services.AddControllersWithViews(x =>
            {
                x.Conventions.Add(new DynamicControllerModelConvention());
            });

            // FIXME: bad practice
            var sp = services.BuildServiceProvider();

            mvc.ConfigureApplicationPartManager(x =>
            {
                var logger = sp.GetRequiredService<ILogger<DynamicControllerFeatureProvider>>();
                var provider = new DynamicControllerFeatureProvider(ontology, logger);

                x.FeatureProviders.Add(provider);
            });

            if (env.IsDevelopment())
                mvc.AddRazorRuntimeCompilation(o => { });

            mvc = services.AddRazorPages();
            if (env.IsDevelopment())
                mvc.AddRazorRuntimeCompilation(o => { });

            services.AddSignalR();
            services.AddRouting(x =>
            {
                x.AppendTrailingSlash = true;
                x.LowercaseUrls = true;
                x.LowercaseQueryStrings = false;
            });

            services.AddMemoryCache(x => { });
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ICacheRegion<>), typeof(InProcessCacheRegion<>)));
            
            services.AddSingleton<IRecordIndex, LunrRecordIndex>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, RebuildIndexOnRecordEvents>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, NotifyHubsWhenRecordAdded>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, InvalidateCachesWhenRecordAdded>());
            services.AddSingleton<RecordEvents>();

            services.AddSingleton<ILogStore, LightningLogStore>();
            services.AddSingleton<IRecordStore>(r =>
            {
                var index = r.GetService<IRecordIndex>();
                
                var store = new LightningRecordStore(Crypto.ToHexString(publicKey), index,
                        r.GetRequiredService<RecordEvents>(), 
                        r.GetRequiredService<ILogger<LightningRecordStore>>());

                return store;
            });

            var change = new OntologyChangeProvider();
            services.AddSingleton(change);
            services.AddSingleton<IActionDescriptorChangeProvider, OntologyChangeProvider>(r => r.GetRequiredService<OntologyChangeProvider>());
            
            services.AddSingleton<ThrottleFilter>();
            services.AddScoped<BaseViewModelFilter>();

            services.AddSingleton<WebServerHostedService>();
            services.AddHostedService(r => r.GetRequiredService<WebServerHostedService>());

            return services;
        }
    }
}
