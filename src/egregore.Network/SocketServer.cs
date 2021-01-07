// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace egregore.Network
{
    public sealed class SocketServer : IDisposable
    {
        private readonly string _id;
        private readonly TimeSpan _interval;
        private readonly TextWriter _out;
        private readonly IProtocol _protocol;

        private readonly ManualResetEvent _stopping = new ManualResetEvent(false);
        private string _address;
        private NetMQSocket _outgoing;
        private CancellationTokenSource _source;

        private Task _task;

        public SocketServer(IProtocol protocol, string id = default, TextWriter @out = default)
        {
            _id = id ?? "[SERVER]";
            _protocol = protocol;
            _out = @out;
            _interval = TimeSpan.FromMilliseconds(100);
        }

        public void Dispose()
        {
            _source?.Cancel(true);
            while (_task != default && !_task.IsCanceled && !_task.IsCompleted && !_task.IsFaulted)
                _stopping.WaitOne(10);
            _outgoing?.Dispose();
            _stopping?.Dispose();
            _task?.Dispose();
        }

        public void Start(int port, CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default)
            {
                _source = new CancellationTokenSource();
                cancellationToken = _source.Token;
            }

            _address = $"tcp://*:{port}";
            _outgoing = new ResponseSocket();

            var options = new SocketOptions(_outgoing);
            _protocol.Configure(options);
            _outgoing.Bind(_address);
            _out?.WriteInfoLine($"{_id}: Bound to {_address}");

            _task = Task.Run(() =>
            {
                EstablishHandshake();

                while (!cancellationToken.IsCancellationRequested)
                    try
                    {
                        if (_outgoing.IsDisposed)
                            continue;
                        if (!_outgoing.TryReceiveFrameBytes(_interval, out var message))
                            continue;
                        _protocol.OnMessageReceived(_outgoing, message);
                    }
                    catch (Exception e)
                    {
                        _out?.WriteErrorLine($"{_id}: Error during message receive loop: {e}");
                        break;
                    }

                _stopping.Set();
            }, cancellationToken);
        }

        private void EstablishHandshake()
        {
            try
            {
                if (!_protocol.Handshake(_outgoing))
                    throw new InvalidOperationException($"{_id}: Handshake failed");
            }
            catch (CryptographicException e)
            {
                _out?.WriteErrorLine($"{_id}: Handshake cryptography is invalid: {e.Message}");
                _source?.Cancel(true);
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: Failed to establish handshake: {e}");
                _source?.Cancel(true);
            }
        }

        public void Send(string message)
        {
            if (_outgoing.IsDisposed)
                return;
            _outgoing.TrySendFrame(_interval, message);
        }

        public void Send(ReadOnlySpan<byte> message)
        {
            if (_outgoing.IsDisposed)
                return;
            var data = message.ToArray();
            _outgoing.TrySendFrame(_interval, data);
        }
    }
}