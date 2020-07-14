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
    }
}