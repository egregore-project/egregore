// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using egregore.Extensions;
using NetMQ;
using NetMQ.Sockets;

namespace egregore.Network
{
    public sealed class SocketClient : IDisposable
    {
        private readonly IProtocol _protocol;
        private readonly TextWriter _out;
        private readonly string _id;
        private readonly RequestSocket _incoming;
        private string _address;

        public SocketClient(IProtocol protocol, string id = default, TextWriter @out = default)
        {
            _id = id ?? "[CLIENT]";
            _protocol = protocol;
            _out = @out;
            _incoming = new RequestSocket();
        }

        public void Connect(string hostName, int port)
        {
            try
            {
                _address = $"tcp://{hostName}:{port}";
                _incoming.Connect(_address);

                _out?.WriteInfoLine($"{_id}: Socket connected to {_address}");
                if (!_protocol.Handshake(_incoming))
                    throw new InvalidOperationException($"{_id}: Handshake failed");
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
                _incoming.Disconnect(_address);
                Dispose();
            }
        }

        public void Disconnect()
        {
            try
            {
                if(!_incoming.IsDisposed)
                    _incoming.Disconnect(_address);
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
                Dispose();
            }
        }

        public byte[] Receive()
        {
            try
            {
                var interval = TimeSpan.FromMilliseconds(100);
                if (!_incoming.TryReceiveFrameBytes(interval, out var message))
                    return message;
                _protocol.OnMessageReceived(_incoming, message);
                return message;
            }
            catch (Exception e)
            {
                _out?.WriteErrorLine($"{_id}: {e}");
                return default;
            }
        }

        public void Send(string message)
        {
            if (_incoming.IsDisposed)
                return;
            _protocol.OnMessageSending(_incoming, Encoding.UTF8.GetBytes(message));
        }

        public void Send(ReadOnlySpan<byte> message)
        {
            if (_incoming.IsDisposed)
                return;
            var data = message.ToArray();
            _protocol.OnMessageSending(_incoming, data);
        }

        public void Dispose()
        {
            if (_incoming == default || _incoming.IsDisposed)
                return;
            _incoming?.Dispose();
        }
    }
}