// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Network;
using egregore.Tests.Helpers;
using Noise;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests.Network
{
    [Collection("Serial")]
    public class NoiseTests
    {
        public NoiseTests(ITestOutputHelper console)
        {
            _console = console;
            _hostName = "localhost";
            _port = 11000;
        }

        private readonly ITestOutputHelper _console;
        private readonly string _hostName;
        private readonly int _port;

        private static unsafe KeyPair GenerateEncryptionKey()
        {
            // convert 64 byte signing key to 32 byte encryption key
            Crypto.GenerateKeyPair(out var spk, out var sk);
            var ek = Crypto.SigningKeyToEncryptionKey(sk);
            NativeMethods.sodium_free(sk);

            // create key pair using the encryption key pair
            var epk = new byte[Crypto.PublicKeyBytes];
            Crypto.EncryptionPublicKeyFromSigningPublicKey(spk, epk);
            var kp = new KeyPair(ek, (int) Crypto.EncryptionKeyBytes, epk);
            return kp;
        }

        [Fact]
        public void Can_handshake_on_connect_and_send_encrypted_payload()
        {
            var @out = new XunitDuplexTextWriter(_console, Console.Out);

            unsafe
            {
                using var ckp = GenerateEncryptionKey();
                using var skp = GenerateEncryptionKey();

                var psk1 = PskRef.Create();
                var psk2 = PskRef.Create(psk1.ptr);

                var sp = new NoiseProtocol(false, skp.PrivateKey, psk1, default, "[SERVER]", @out);
                using var server = new SocketServer(sp, default, @out);
                server.Start(_port);

                var cp = new NoiseProtocol(true, ckp.PrivateKey, psk2, skp.PublicKey, "[CLIENT]", @out);
                var client = new SocketClient(cp, default, @out);
                client.Connect(_hostName, _port);
                client.Send("This is an encrypted message");
                client.Receive();
                client.Disconnect();
            }
        }
    }
}