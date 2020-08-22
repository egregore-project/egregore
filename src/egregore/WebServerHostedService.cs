// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Configuration;
using egregore.Data;
using egregore.Hubs;
using egregore.Network;
using egregore.Ontology;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace egregore
{
    public sealed class WebServerHostedService : IHostedService, IDisposable
    {
        private readonly PeerBus _bus;
        private readonly IOntologyLog _ontology;
        private readonly OntologyChangeProvider _change;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ILogger<WebServerHostedService> _logger;
        private readonly IOptionsMonitor<WebServerOptions> _options;
        private readonly ILogStore _logs;
        private readonly IRecordStore _records;
        private readonly IEnumerable<IRecordListener> _listeners;

        private Timer _timer;
        private long _head = -1;

        public WebServerHostedService(PeerBus bus, IOntologyLog ontology, ILogStore logs, IRecordStore records, IEnumerable<IRecordListener> listeners, OntologyChangeProvider change, IHubContext<NotificationHub> hub, IOptionsMonitor<WebServerOptions> options, ILogger<WebServerHostedService> logger)
        {
            _bus = bus;
            _ontology = ontology;
            _logs = logs;
            _records = records;
            _listeners = listeners;
            _change = change;
            _hub = hub;
            _options = options;
            _logger = logger;
        }

        public void OnOntologyChanged(long index) => Interlocked.Exchange(ref _head, index);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_ontology.Exists(_options.CurrentValue.EggPath))
            {
                _logger?.LogWarning("Could not find ontology log at '{EggPath}'", _options.CurrentValue.EggPath);
            }
             
            try
            {
                var owner = Crypto.ToHexString(_options.CurrentValue.PublicKey);
                
                _logs.Init(_options.CurrentValue.EggPath);
                _ontology.Init(_options.CurrentValue.PublicKey);
                _records.Init(Path.Combine(Constants.DefaultRootPath, $"{owner}.egg"));

                foreach (var listener in _listeners)
                    await listener.OnRecordsInitAsync();

                DutyCycle();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to restore ontology logs: {e}");
                _logger?.LogError(e, "Failed to restore ontology logs");
            }

            _timer = new Timer(DutyCycle, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(5));
        }

        private void DutyCycle(object state = default)
        { 
            if (Interlocked.Read(ref _head) == _ontology.Index)
                return;

            lock(this)
            {
                if (Interlocked.Read(ref _head) == _ontology.Index)
                    return;

                _logger?.LogDebug("Restoring ontology log started for '{EggPath}'", _options.CurrentValue.EggPath);

                if (Interlocked.Read(ref _head) == -1)
                    _logger?.LogDebug("Initializing ontology log");

                var sw = Stopwatch.StartNew();
                _ontology.Materialize(_logs, _hub, _change);
                Interlocked.Exchange(ref _head, _ontology.Index);
                _logger?.LogDebug($"Restoring ontology log completed ({sw.Elapsed.TotalMilliseconds}ms)");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Interlocked.Exchange(ref _head, long.MaxValue);
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