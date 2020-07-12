// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Sockets;

namespace egregore.Network
{
    public interface IProtocol
    {
        void OnMessageReceived(IProtocolSend sender, Socket handler, string message);
        bool IsEndOfMessage(string message);
    }
}