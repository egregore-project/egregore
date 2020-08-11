// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Hubs;
using egregore.Network;
using egregore.Ontology;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore
{
    internal sealed class WebServerHostedService : IHostedService, IDisposable
    {
        private readonly PeerBus _bus;
        private readonly IOntologyLog _ontology;
        private readonly OntologyChangeProvider _change;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ILogger<WebServerHostedService> _logger;
        private readonly IOptionsMonitor<WebServerOptions> _options;
        
        private Timer _timer;
        private ILogStore _store;
        private long _index = -1;

        public WebServerHostedService(PeerBus bus, IOntologyLog ontology, OntologyChangeProvider change, IHubContext<NotificationHub> hub, IOptionsMonitor<WebServerOptions> options, ILogger<WebServerHostedService> logger)
        {
            _bus = bus;
            _ontology = ontology;
            _change = change;
            _hub = hub;
            _options = options;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_ontology.Exists(_options.CurrentValue.EggPath))
            {
                _logger?.LogWarning("Could not find ontology log at '{EggPath}', skipping", _options.CurrentValue.EggPath);
                return Task.CompletedTask;
            }
             
            try
            {
                _store = new LightningLogStore(_options.CurrentValue.EggPath);
                _store.Init();
                _ontology?.Init(_options.CurrentValue.PublicKey);
                DutyCycle();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to restore ontology logs: {e}");
                _logger?.LogError(e, "Failed to restore ontology logs");
            }

            _timer = new Timer(DutyCycle, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DutyCycle(object state = default)
        { 
            if (Interlocked.Read(ref _index) >= _ontology.Index)
                return;

            lock(this)
            {
                if (Interlocked.Read(ref _index) >= _ontology.Index)
                    return;

                _logger?.LogDebug("Restoring ontology logs started for '{EggPath}'", _options.CurrentValue.EggPath);
                _ontology.Materialize(_store, _hub, _change);
                _logger?.LogDebug("Restoring ontology logs completed");

                Interlocked.Exchange(ref _index, _ontology.Index);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Interlocked.Exchange(ref _index, long.MaxValue);
                _timer?.Change(Timeout.Infinite, 0);
                _logger?.LogDebug("Cleaning up peer bus...");
                _bus?.Dispose();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error occurred during peer bus cleanup: {e}");
                _logger?.LogError(e, "Error occurred during peer bus cleanup");
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}