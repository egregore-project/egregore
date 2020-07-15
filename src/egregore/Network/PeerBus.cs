// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using egregore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;

namespace egregore.Network
{
    /// <summary>
    /// Originally based on NetMQ beacon example: https://netmq.readthedocs.io/en/latest/beacon/
    /// </summary>
    internal sealed class PeerBus : IDisposable
    {
        public const string PublishCommand = "P";
        public const string GetHostAddressCommand = "GetHostAddress";
        
        private readonly TimeSpan _lifetime = TimeSpan.FromSeconds(10);
        
        private readonly NetMQActor _actor;
        private PublisherSocket _publisher;
        private SubscriberSocket _subscriber;

        private NetMQBeacon _beacon;
        private NetMQPoller _poll;
        private PairSocket _shim;
        private int _port;

        private readonly Dictionary<PeerInfo, DateTimeOffset> _info;
        private readonly int _beaconPort;
        private readonly ILogger<PeerBus> _logger;
        private readonly string _id;

        public PeerBus(IOptions<WebServerOptions> options, ILogger<PeerBus> logger)
        {
            _id = $"[{options.Value.ServerId}]";
            _info = new Dictionary<PeerInfo, DateTimeOffset>();
            _beaconPort = options.Value.BeaconPort;
            _logger = logger;
            _actor = NetMQActor.Create(RunActor);
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
                    _logger?.LogInformation($"{_id}: Bus subscriber is bound to {{SubscriberPort}}", _subscriber.Options.LastEndpoint);
                    _subscriber.ReceiveReady += OnSubscriberReady;

                    _logger?.LogInformation($"{_id}: Beacon is being configured to UDP port {{BeaconPort}}", _beaconPort);
                    _beacon.Configure(_beaconPort);

                    _logger?.LogInformation($"{_id}: Beacon is publishing the Bus subscriber port {{BusPort}}", _subscriber.Options.LastEndpoint);
                    _beacon.Publish(_port.ToString(), TimeSpan.FromSeconds(1));

                    _logger?.LogInformation($"{_id}: Beacon is subscribing to all beacons on UDP port {{BeaconPort}}", _beaconPort);
                    _beacon.Subscribe(string.Empty);
                    _beacon.ReceiveReady += OnBeaconReady;

                    var cleanupTimer = new NetMQTimer(TimeSpan.FromSeconds(1));
                    cleanupTimer.Elapsed += Cleanup;

                    _poll = new NetMQPoller { _shim, _subscriber, _beacon, cleanupTimer };
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
                _logger?.LogInformation($"{_id}: Adding new peer from '{{PeerAddress}}'", peer.Address);
                _info.Add(peer, readyTime);
                _publisher.Connect(peer.Address);
                _shim.SendMoreFrame("A").SendFrame(peer.Address);
            }
            else
            {
                _info[peer] = readyTime;
            }
        }

        private void Cleanup(object sender, NetMQTimerEventArgs e)
        {
            var unresponsivePeers = _info.
                Where(n => DateTimeOffset.Now > n.Value + _lifetime)
                .Select(n => n.Key).ToArray();

            foreach (var peer in unresponsivePeers)
            {
                _logger?.LogInformation($"{_id}: Removing unresponsive peer at '{{PeerAddress}}'", peer.Address);
                _info.Remove(peer);
                _publisher.Disconnect(peer.Address);
                _shim.SendMoreFrame("R").SendFrame(peer.Address);
            }
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
    }
}