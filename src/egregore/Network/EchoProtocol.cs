// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Sockets;
using egregore.Extensions;

namespace egregore.Network
{
    /// <summary>
    /// The simplest possible protocol that takes 
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

        public void OnMessageReceived(IProtocolSend sender, Socket handler, string message)
        {
            _out?.WriteInfoLine($"{_id}: {message}");
            sender.Send(handler, message);
        }

        public bool IsEndOfMessage(string message) => message.IndexOf(EndOfMessage, StringComparison.Ordinal) > -1;
    }
}