// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        
        private PublisherSocket _publisher;
        private SubscriberSocket _subscriber;
        private NetMQBeacon _beacon;
        private NetMQPoller _poll;
        private PairSocket _shim;
        private int _port;

        private readonly TimeSpan _peerLifetime = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _beaconInterval = TimeSpan.FromSeconds(1);
        private readonly NetMQActor _actor;
        
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
                    _logger?.LogInformation($"{_id}: Peer bus is bound to {{BusPort}}", _port);
                    _subscriber.ReceiveReady += OnSubscriberReady;

                    _logger?.LogInformation($"{_id}: Peer is broadcasting UDP on port {{BeaconPort}}", _beaconPort);
                    _beacon.Configure(_beaconPort);
                    _beacon.Publish(_port.ToString(), _beaconInterval);
                    _beacon.Subscribe(string.Empty);
                    _beacon.ReceiveReady += OnBeaconReady;

                    var cleanupTimer = new NetMQTimer(_cleanupInterval);
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
                _logger?.LogDebug($"{_id}: Updating keep-alive for peer '{{PeerAddress}}'", peer.Address);
                _info[peer] = readyTime;
            }
        }

        private void Cleanup(object sender, NetMQTimerEventArgs e)
        {
            _logger?.LogDebug($"{_id}: Running peer cleanup on thread {Thread.CurrentThread.Name}"); 

            var unresponsivePeers = _info.
                Where(n => DateTimeOffset.Now > n.Value + _peerLifetime)
                .Select(n => n.Key);

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