// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using egregore.Extensions;
using NetMQ;

namespace egregore.Network
{
    /// <summary>
    /// The simplest possible protocol that takes delimited input and echos it back to the connected client.
    /// </summary>
    internal sealed class EchoProtocol : IProtocol
    {
        private readonly bool _initiator;
        private readonly TextWriter _out;
        private readonly string _id;

        public EchoProtocol(bool initiator, string id = default, TextWriter @out = default)
        {
            _initiator = initiator;
            _out = @out;
            _id = id ?? "[ECHO]";
        }

        public void Configure(SocketOptions options) { }
        public bool Handshake(NetMQSocket handler) => true;
        
        public void OnMessageReceived(NetMQSocket handler, ReadOnlySpan<byte> payload)
        {
            if (_initiator)
                return;
            var data = payload.ToArray();
            handler.SendFrame(data);
            _out?.WriteInfoLine($"{_id}: Sent: '{Encoding.UTF8.GetString(payload)}'");
        }

        public void OnMessageSending(NetMQSocket handler, ReadOnlySpan<byte> payload)
        {
            var data = payload.ToArray();
            handler.SendFrame(data);
        }
    }
}