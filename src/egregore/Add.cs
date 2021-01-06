// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using egregore.Caching;
using egregore.Configuration;
using egregore.Data;
using egregore.Events;
using egregore.Filters;
using egregore.Identity;
using egregore.IO;
using egregore.Media;
using egregore.Network;
using egregore.Ontology;
using egregore.Pages;
using egregore.Search;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace egregore
{
    internal static class Add
    {
        private static MemoryOntologyLog _ontology;

        public static unsafe IServiceCollection AddWebServer(this IServiceCollection services, string eggPath,
            IKeyCapture capture, IWebHostEnvironment env, IConfiguration config, IWebHostBuilder webBuilder)
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
                if (NativeMethods.crypto_generichash(id, fingerprint.Length, pk, Crypto.PublicKeyBytes, key,
                    app.Length) != 0)
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

            AddEvents(services);

            AddDataStores(services);

            AddFilters(services);

            AddDaemonService(services);

            var change = new DynamicActionDescriptorChangeProvider();
            services.AddSingleton(change);
            services.AddSingleton<IActionDescriptorChangeProvider, DynamicActionDescriptorChangeProvider>(r => r.GetRequiredService<DynamicActionDescriptorChangeProvider>());
            services.AddSingleton<ISearchIndex, LunrRecordIndex>();
            services.AddSingleton<PeerBus>();
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

            AddIdentity(services);
            return services;
        }

        private static void AddIdentity(IServiceCollection services)
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
        }

        private static void AddDaemonService(IServiceCollection services)
        {
            services.AddSingleton<WebServerHostedService>();
            services.AddSingleton<IOntologyChangeHandler>(r => r.GetRequiredService<WebServerHostedService>());
            services.AddHostedService(r => r.GetRequiredService<WebServerHostedService>());
        }

        private static void AddDataStores(IServiceCollection services)
        {
            services.AddSingleton<ILogObjectTypeProvider, LogObjectTypeProvider>();
            services.AddSingleton<ILogEntryHashProvider, LogEntryHashProvider>();
            services.AddScoped<OntologyWriter>();

            services.AddSingleton<ILogStore, LightningLogStore>();
            services.AddSingleton<IMediaStore, LightningMediaStore>();
            services.AddSingleton<IRecordStore, LightningRecordStore>();
            services.AddSingleton<IPageStore, LightningPageStore>();
        }

        private static void AddEvents(IServiceCollection services)
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