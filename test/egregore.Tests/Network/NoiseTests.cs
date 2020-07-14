// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Network;
using Noise;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Network
{
    public class NoiseTests
    {
        private readonly ITestOutputHelper _console;
        private readonly string _hostName;
        private readonly int _port;

        public NoiseTests(ITestOutputHelper console)
        {
            _console = console;
            _hostName = "localhost";
            _port = 11000;
        }
        
        [Fact]
        public void Can_handshake_on_connect()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);

            using var clientKeyPair = KeyPair.Generate();
            using var serverKeyPair = KeyPair.Generate();
            unsafe
            {
                var psk1 = PskRef.Create();
                var psk2 = PskRef.Create(psk1.ptr);

                var sp = new NoiseProtocol(false, serverKeyPair.PrivateKey, psk1.ptr);
                using var server = new SocketServer(sp, default, @out);
                server.Start(_hostName, _port);

                var cp = new NoiseProtocol(true, clientKeyPair.PrivateKey, psk2.ptr, serverKeyPair.PublicKey);
                var client = new SocketClient(cp, default, @out);
                client.Connect(_hostName, _port);
                client.Disconnect();
            }
        }
    }
}