// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Network;
using egregore.Ontology;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal sealed class WebServerStartup : IHostedService
    {
        private readonly PeerBus _bus;

        private readonly ILogger<WebServerStartup> _logger;
        private readonly IOptionsMonitor<WebServerOptions> _options;
        private OntologyLog _ontology;

        public WebServerStartup(PeerBus bus, IOptionsMonitor<WebServerOptions> options,
            ILogger<WebServerStartup> logger)
        {
            _bus = bus;
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
            try
            {
                _logger.LogDebug("Cleaning up peer bus...");
                _bus?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred during peer bus cleanup");
            }

            return Task.CompletedTask;
        }
    }
}