// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Sockets;

namespace egregore.Network
{
    public interface IProtocol
    {
        bool Handshake(IChannel sender, Socket handler);
        void OnMessageReceived(IChannel sender, Socket handler, ReadOnlySpan<byte> message);
        bool IsEndOfMessage(string message);
        bool HasTransport { get; }
    }
}