// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Sockets;

namespace egregore.Network
{
    public interface IProtocolSend
    {
        void Send(Socket handler, string message);
    }
}