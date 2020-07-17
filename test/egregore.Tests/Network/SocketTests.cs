// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Network;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Network
{
    public class SocketTests
    {
        public SocketTests(ITestOutputHelper console)
        {
            _console = console;
            _hostName = "localhost";
            _port = 11000;
        }

        private readonly ITestOutputHelper _console;
        private readonly string _hostName;
        private readonly int _port;

        [Fact]
        public void Can_send_and_receive_from_socket()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);

            using var server = new SocketServer(new EchoProtocol(false, default, @out), default, @out);
            server.Start(_port);

            using var client = new SocketClient(new EchoProtocol(true, default, @out), default, @out);
            client.Connect(_hostName, _port);

            client.Send("This is a test 1");
            client.Receive();

            client.Send("This is a test 2");
            client.Receive();

            client.Send("This is a test 3");
            client.Receive();

            client.Disconnect();
        }
    }
}