// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using egregore.Extensions;

namespace egregore.Network
{
    /// <summary>
    /// The simplest possible protocol that takes delimited input and echos it back to the connected client.
    /// </summary>
    internal sealed class EchoProtocol : IProtocol
    {
        private const string EndOfMessage = "<EOF>";

        private readonly TextWriter _out;
        private readonly string _id;

        public EchoProtocol(TextWriter @out = default)
        {
            _out = @out;
            _id = "[ECHO]";
        }

        public bool Handshake(IChannel sender, Socket handler) => true;
        public bool HasTransport => true;

        public void OnMessageReceived(IChannel sender, Socket handler, ReadOnlySpan<byte> message)
        {
            _out?.WriteInfoLine($"{_id}: {Encoding.UTF8.GetString(message)}");
            sender.Send(handler, message);
        }

        public bool IsEndOfMessage(string message) => message.IndexOf(EndOfMessage, StringComparison.Ordinal) > -1;
    }
}