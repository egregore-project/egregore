// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Ontology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal sealed class WebServerStartup : IHostedService
    {
        private readonly ILogger<WebServerStartup> _logger;
        private readonly IOptionsMonitor<WebServerOptions> _options;
        private OntologyLog _ontology;

        public WebServerStartup(IOptionsMonitor<WebServerOptions> options, ILogger<WebServerStartup> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_options.CurrentValue.EggPath))
                return Task.CompletedTask;

            try
            {
                _logger?.LogInformation($"Restoring ontology logs started for '{_options.CurrentValue.EggPath}'");
                var store = new LightningLogStore(_options.CurrentValue.EggPath);
                store.Init();

                var owner = _options.CurrentValue.PublicKey;
                _ontology = new OntologyLog(owner);
                _logger?.LogInformation("Restoring ontology logs completed");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to restore ontology logs");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}