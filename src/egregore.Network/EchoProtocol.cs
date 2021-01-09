// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Text;
using NetMQ;

namespace egregore.Network
{
    /// <summary>
    ///     The simplest possible protocol that takes delimited input and echos it back to the connected client.
    /// </summary>
    public sealed class EchoProtocol : IProtocol
    {
        private readonly string _id;
        private readonly bool _initiator;
        private readonly TextWriter _out;

        public EchoProtocol(bool initiator, string id = default, TextWriter @out = default)
        {
            _initiator = initiator;
            _out = @out;
            _id = id ?? "[ECHO]";
        }

        public void Configure(SocketOptions options)
        {
        }

        public bool Handshake(NetMQSocket handler)
        {
            return true;
        }

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