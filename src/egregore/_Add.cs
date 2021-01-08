// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using egregore.Configuration;
using egregore.Cryptography;
using egregore.Data;
using egregore.Filters;
using egregore.Identity;
using egregore.Models;
using egregore.Models.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace egregore
{
    internal static class Add
    {
        private static MemoryOntologyLog _ontology;

        public static IServiceCollection AddWebServer(this IServiceCollection services, string eggPath, int port, IKeyCapture capture, IWebHostEnvironment env, IConfiguration config)
        {
            var publicKeyString = AddPublicKeyIdentifier(services, eggPath, port, capture, env, config);

            services.AddAntiforgery(o =>
            {
                o.Cookie.Name = $"_{publicKeyString}_xsrf";
                o.Cookie.HttpOnly = true;
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.HeaderName = "X-XSRF-Token";
            });
            
            return services;
        }

        public static string AddPublicKeyIdentifier(this IServiceCollection services, string eggPath, int port, IKeyCapture capture, IWebHostEnvironment env, IConfiguration config)
        {
            var keyFileService = new ServerKeyFileService();
            services.AddSingleton<IKeyFileService>(keyFileService);

            capture ??= new ServerConsoleKeyCapture();
            services.AddSingleton(capture);

            if (capture is IPersistedKeyCapture persisted)
                services.AddSingleton(persisted);

            var publicKey = Crypto.SigningPublicKeyFromSigningKey(keyFileService, capture);
            capture.Reset();

            var appString = $"{env.ApplicationName}:" +
                            $"{env.EnvironmentName}:" +
                            $"{port}";

            var serverId = Crypto.Fingerprint(publicKey, appString);
            var publicKeyString = Crypto.ToHexString(publicKey);

            services.Configure<WebServerOptions>(config.GetSection("WebServer"));
            services.Configure<WebServerOptions>(o =>
            {
                o.PublicKey = publicKey;
                o.ServerPort = port;
                o.PublicKeyString = publicKeyString;
                o.ServerId = serverId;
                o.EggPath = eggPath;
            });

            return publicKeyString;
        }

        public static IServiceCollection AddCacheRegions(this IServiceCollection services)
        {
            services.AddMemoryCache(x => { });
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ICacheRegion<>), typeof(InProcessCacheRegion<>)));
            return services;
        }

        public static IServiceCollection AddDynamicControllers(this IServiceCollection services, IWebHostEnvironment env)
        {
            var change = new DynamicActionDescriptorChangeProvider();
            services.AddSingleton(change);
            services.AddSingleton<IActionDescriptorChangeProvider, DynamicActionDescriptorChangeProvider>(r => r.GetRequiredService<DynamicActionDescriptorChangeProvider>());

            // FIXME: bad practice
            var sp = services.BuildServiceProvider();

            var mvc = services.AddControllersWithViews(x =>
            {
                x.Conventions.Add(new DynamicControllerModelConvention());
            });
            
            mvc.ConfigureApplicationPartManager(x =>
            {
                var logger = sp.GetRequiredService<ILogger<DynamicControllerFeatureProvider>>();
                var ontology = sp.GetRequiredService<IOntologyLog>();
                var provider = new DynamicControllerFeatureProvider(ontology, logger);
                x.FeatureProviders.Add(provider);
            });

            if (env.IsDevelopment())
            {
                mvc.AddRazorRuntimeCompilation(o => { });
            }

            services.AddRazorPages();
            
            return services;
        }

        public static IServiceCollection AddIdentity(this IServiceCollection services)
        {
            // FIXME: distinguish between app and api

            services
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddUserStore<UserStore>()
                .AddRoleStore<RoleStore>()
                .AddDefaultTokenProviders();

            services.AddAuthentication()
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = "https://localhost:5001",
                        ValidAudience = "https://localhost:5001",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("0123456789abcdef"))
                    };
                });

            return services;
        }

        public static IServiceCollection AddDaemonServices(this IServiceCollection services)
        {
            services.AddSingleton<DaemonService>();
            services.AddSingleton<IOntologyChangeHandler>(r => r.GetRequiredService<DaemonService>());
            services.AddHostedService(r => r.GetRequiredService<DaemonService>());

            services.AddSingleton<PeerBus>();
            services.AddSingleton(r =>
            {
                // This is static because the hosted service is called on its own thread, which would otherwise duplicate the log
                _ontology ??= new MemoryOntologyLog(r.GetRequiredService<OntologyEvents>(), r.GetRequiredService<IOptions<WebServerOptions>>().Value.PublicKey);
                return _ontology;
            });
            services.AddTransient<IOntologyLog>(r => r.GetRequiredService<MemoryOntologyLog>());
            return services;
        }

        public static IServiceCollection AddDataStores(this IServiceCollection services)
        {
            services.AddSingleton<ILogObjectTypeProvider, LogObjectTypeProvider>();
            services.AddSingleton<ILogEntryHashProvider, LogEntryHashProvider>();
            services.AddScoped<OntologyWriter>();

            services.AddSingleton<ILogStore, LightningLogStore>();
            services.AddSingleton<IMediaStore, LightningMediaStore>();
            services.AddSingleton<IPageStore, LightningPageStore>();
            services.AddSingleton<IRecordStore, LightningRecordStore>(r =>
                new LightningRecordStore(r.GetRequiredService<IOptions<WebServerOptions>>().Value.PublicKeyString,
                    r.GetRequiredService<ISearchIndex>(), r.GetRequiredService<RecordEvents>(),
                    r.GetRequiredService<ILogObjectTypeProvider>(),
                    r.GetRequiredService<ILogger<LightningRecordStore>>()));

            return services;
        }

        public static IServiceCollection AddEvents(this IServiceCollection services)
        {
            services.AddSingleton<RecordEvents>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, RebuildIndexOnRecordEvents>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, NotifyHubsWhenRecordAdded>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRecordEventHandler, InvalidateCachesWhenRecordAdded>());

            services.AddSingleton<OntologyEvents>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IOntologyEventHandler, RebuildControllersWhenSchemaAdded>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IOntologyEventHandler, NotifyHubsWhenSchemaAdded>());

            services.AddSingleton<MediaEvents>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMediaEventHandler, NotifyHubsWhenMediaAdded>());

            services.AddSingleton<PageEvents>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPageEventHandler, NotifyHubsWhenPageAdded>());

            return services;
        }

        public static IServiceCollection AddFilters(this IServiceCollection services)
        {
            services.AddSingleton<ThrottleFilter>();
            services.AddSingleton<OntologyFilter>();
            services.AddSingleton<RemoteAddressFilter>();
            services.AddScoped<BaseViewModelFilter>();

            return services;
        }

        public static IServiceCollection AddSearchIndexes(this IServiceCollection services)
        {
            services.AddSingleton<ISearchIndex, LunrRecordIndex>();
            return services;
        }
    }
}