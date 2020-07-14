// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NetMQ;

namespace egregore.Network
{
    public interface IProtocol
    {
        void Configure(SocketOptions options);
        bool Handshake(NetMQSocket handler);

        void OnMessageReceived(NetMQSocket handler, ReadOnlySpan<byte> payload);
        void OnMessageSending(NetMQSocket handler, ReadOnlySpan<byte> payload);
    }
}