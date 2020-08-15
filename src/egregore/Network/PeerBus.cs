// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using egregore.Configuration;
using egregore.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;

namespace egregore.Network
{
    /// <summary>
    ///     Originally based on NetMQ beacon example: https://netmq.readthedocs.io/en/latest/beacon/
    /// </summary>
    public sealed class PeerBus : IDisposable
    {
        public const string PublishCommand = "P";
        public const string GetHostAddressCommand = "GetHostAddress";
        private readonly NetMQActor _actor;
        private readonly TimeSpan _beaconInterval = TimeSpan.FromSeconds(1);
        private readonly int _beaconPort;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(5);
        private readonly IHubContext<NotificationHub> _hub;
        private readonly string _id;

        private readonly Dictionary<PeerInfo, DateTimeOffset> _info;
        private readonly ILogger<PeerBus> _logger;

        private readonly TimeSpan _peerLifetime = TimeSpan.FromSeconds(10);
        private NetMQBeacon _beacon;
        private NetMQPoller _poll;
        private int _port;

        private PublisherSocket _publisher;
        private PairSocket _shim;
        private SubscriberSocket _subscriber;

        public PeerBus(IHubContext<NotificationHub> hub, IOptions<WebServerOptions> options, ILogger<PeerBus> logger)
        {
            _id = $"[{options.Value.ServerId}]";
            _info = new Dictionary<PeerInfo, DateTimeOffset>();
            _beaconPort = options.Value.BeaconPort;
            _hub = hub;
            _logger = logger;
            _actor = NetMQActor.Create(RunActor);
        }

        public void Dispose()
        {
            _actor?.Dispose();
            _publisher?.Dispose();
            _subscriber?.Dispose();
            _beacon?.Dispose();
            _poll?.Dispose();
            _shim?.Dispose();
        }

        private void RunActor(PairSocket shim)
        {
            _shim = shim;

            using (_subscriber = new SubscriberSocket())
            using (_publisher = new PublisherSocket())
            {
                using (_beacon = new NetMQBeacon())
                {
                    _shim.ReceiveReady += OnShimReady;

                    _subscriber.Subscribe(string.Empty);
                    _port = _subscriber.BindRandomPort("tcp://*");
                    _logger?.LogInformation($"{_id}: Peer bus is bound to {{BusPort}}", _port);
                    _subscriber.ReceiveReady += OnSubscriberReady;

                    _logger?.LogInformation($"{_id}: Peer is broadcasting UDP on port {{BeaconPort}}", _beaconPort);
                    _beacon.Configure(_beaconPort);
                    _beacon.Publish(_port.ToString(), _beaconInterval);
                    _beacon.Subscribe(string.Empty);
                    _beacon.ReceiveReady += OnBeaconReady;

                    var cleanupTimer = new NetMQTimer(_cleanupInterval);
                    cleanupTimer.Elapsed += Cleanup;

                    _poll = new NetMQPoller {_shim, _subscriber, _beacon, cleanupTimer};
                    _shim.SignalOK();
                    _poll.Run();
                }
            }
        }

        private void OnShimReady(object sender, NetMQSocketEventArgs e)
        {
            var command = _shim.ReceiveFrameString();
            switch (command)
            {
                case NetMQActor.EndShimMessage:
                {
                    _poll.Stop();
                    break;
                }
                case PublishCommand:
                {
                    _publisher.SendMultipartMessage(_shim.ReceiveMultipartMessage());
                    break;
                }
                case GetHostAddressCommand:
                {
                    _shim.SendFrame($"{_beacon.BoundTo}:{_port}");
                    break;
                }
            }
        }

        private void OnSubscriberReady(object sender, NetMQSocketEventArgs e)
        {
            _shim.SendMultipartMessage(_subscriber.ReceiveMultipartMessage());
        }

        private void OnBeaconReady(object sender, NetMQBeaconEventArgs e)
        {
            var readyTime = DateTimeOffset.Now;

            var message = _beacon.Receive();
            int.TryParse(message.String, out var port);

            var peer = new PeerInfo(message.PeerHost, port);
            if (!_info.ContainsKey(peer))
            {
                _info.Add(peer, readyTime);
                _publisher.Connect(peer.Address);
                _shim.SendMoreFrame("A").SendFrame(peer.Address);

                _logger?.LogInformation($"{_id}: Added new peer from '{{PeerAddress}}'", peer.Address);
                _hub?.Clients.All.SendAsync("ReceiveMessage", "success", $"Adding new peer from '{peer.Address}'");
            }
            else
            {
                _logger?.LogDebug($"{_id}: Updating keep-alive for peer '{{PeerAddress}}'", peer.Address);
                _info[peer] = readyTime;
            }
        }

        private void Cleanup(object sender, NetMQTimerEventArgs e)
        {
            _logger?.LogDebug($"{_id}: Running peer cleanup on thread {Thread.CurrentThread.Name}");

            var unresponsivePeers = _info.Where(n => DateTimeOffset.Now > n.Value + _peerLifetime)
                .Select(n => n.Key);

            foreach (var peer in unresponsivePeers)
            {
                _info.Remove(peer);
                _publisher.Disconnect(peer.Address);
                _shim.SendMoreFrame("R").SendFrame(peer.Address);

                _logger?.LogInformation($"{_id}: Removed unresponsive peer at '{{PeerAddress}}'", peer.Address);
                _hub?.Clients.All.SendAsync("ReceiveMessage", "warning",
                    $"Removing unresponsive peer at '{peer.Address}'");
            }
        }
    }
}