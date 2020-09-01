using System;
using System.Text;
using egregore.Caching;
using egregore.Configuration;
using egregore.Data;
using egregore.Events;
using egregore.Filters;
using egregore.IO;
using egregore.Media;
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
using Microsoft.Extensions.Options;

namespace egregore
{
    internal static class Add
    {
        private static MemoryOntologyLog _ontology;

        public static unsafe IServiceCollection AddWebServer(this IServiceCollection services, string eggPath, IKeyCapture capture, IWebHostEnvironment env, IConfiguration config, IWebHostBuilder webBuilder)
        {
            services.AddCors(x => x.AddDefaultPolicy(b =>
            {
                b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                b.DisallowCredentials(); // credentials are invalid when origin is *
            }));

            services.AddSignalR();

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
                o.PublicKeyString = Crypto.ToHexString(publicKey);
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

            var mvc = services.AddControllersWithViews(x =>
            {
                x.Conventions.Add(new DynamicControllerModelConvention());
            });

            var change = new OntologyChangeProvider();
            services.AddSingleton(change);
            services.AddSingleton<IActionDescriptorChangeProvider, OntologyChangeProvider>(r => r.GetRequiredService<OntologyChangeProvider>());
            services.AddSingleton<IRecordIndex, LunrRecordIndex>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, RebuildIndexOnRecordEvents>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, NotifyHubsWhenRecordAdded>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, InvalidateCachesWhenRecordAdded>());
            services.AddSingleton<RecordEvents>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IOntologyEventHandler, RebuildControllersWhenSchemaAdded>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IOntologyEventHandler, NotifyHubsWhenSchemaAdded>());
            services.AddSingleton<OntologyEvents>();

            services.AddSingleton<PeerBus>();
            services.AddSingleton<OntologyEvents>();
            services.AddSingleton(r =>
            {
                // This is static because the hosted service is called on its own thread, which would otherwise duplicate the log
                _ontology ??= new MemoryOntologyLog(r.GetRequiredService<OntologyEvents>(), publicKey);
                return _ontology;
            });
            services.AddTransient<IOntologyLog>(r => r.GetRequiredService<MemoryOntologyLog>());
            
            // FIXME: bad practice
            var sp = services.BuildServiceProvider();
            mvc.ConfigureApplicationPartManager(x =>
            {
                var logger = sp.GetRequiredService<ILogger<DynamicControllerFeatureProvider>>();
                var ontology = sp.GetRequiredService<IOntologyLog>();
                var provider = new DynamicControllerFeatureProvider(ontology, logger);

                x.FeatureProviders.Add(provider);
            });

            if (env.IsDevelopment())
                mvc.AddRazorRuntimeCompilation(o => { });

            mvc = services.AddRazorPages();
            if (env.IsDevelopment())
                mvc.AddRazorRuntimeCompilation(o => { });

            services.AddRouting(x =>
            {
                x.AppendTrailingSlash = true;
                x.LowercaseUrls = true;
                x.LowercaseQueryStrings = false;
            });

            services.AddMemoryCache(x => { });
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ICacheRegion<>), typeof(InProcessCacheRegion<>)));
            
            AddDataStores(services);

            AddFilters(services);

            services.AddSingleton<WebServerHostedService>();
            services.AddHostedService(r => r.GetRequiredService<WebServerHostedService>());

            return services;
        }

        private static void AddDataStores(IServiceCollection services)
        {
            services.AddSingleton<ILogStore, LightningLogStore>();
            services.AddSingleton<IMediaStore, LightningMediaStore>();
            services.AddSingleton<IRecordStore, LightningRecordStore>();
        }

        private static void AddFilters(IServiceCollection services)
        {
            services.AddSingleton<ThrottleFilter>();
            services.AddSingleton<OntologyFilter>();
            services.AddSingleton<RemoteAddressFilter>();
            services.AddScoped<BaseViewModelFilter>();
        }
    }
}
