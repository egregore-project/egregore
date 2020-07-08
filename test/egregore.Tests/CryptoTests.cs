﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using egregore.IO;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public class CryptoTests
    {
        private readonly ITestOutputHelper _output;

        public CryptoTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Can_fill_buffer_with_random_bytes()
        {
            var target = new byte[256U];
            var empty = new byte[256U];
            Assert.True(target.SequenceEqual(empty));

            Crypto.FillNonZeroBytes(target);
            Assert.False(target.SequenceEqual(empty));
        }

        [Fact]
        public void Can_partially_fill_buffer_with_random_bytes()
        {
            var target = new byte[256U];
            var empty = new byte[128U];
            Crypto.FillNonZeroBytes(target, 128U);
            Assert.True(target.Skip(128).SequenceEqual(empty)); // second half empty
            Assert.False(target.Take(128).SequenceEqual(empty)); // first half full
        }

        [Fact]
        public void Can_generate_nonce()
        {
            var nonce1 = Crypto.Nonce(64U);
            var nonce2 = Crypto.Nonce(64U);
            Assert.False(nonce1.SequenceEqual(nonce2));
        }

        [Fact]
        public void Can_generate_key_pair()
        {
            unsafe
            {
                Crypto.GenerateKeyPair(out var pk, out var sk);
                Assert.NotEmpty(pk);
                Assert.False(sk == default(byte*));    
            }
        }

        [Fact]
        public void Can_round_trip_binary_to_hex_string()
        {
            var buffer1 = Encoding.UTF8.GetBytes("rosebud");
            var hexString = Crypto.ToHexString(buffer1);
            Assert.NotEmpty(hexString);

            var buffer2 = hexString.ToBinary();
            Assert.True(buffer1.SequenceEqual(buffer2));
        }

        [Fact]
        public void Can_swap_signing_key_for_encryption_key()
        {
            unsafe
            {
                var capture = new PlaintextKeyCapture("rosebud", "rosebud");
                var service = new TempKeyFileService();
                CryptoTestHarness.GenerateKeyFile(_output, capture, service);
                capture.Reset();
                var sk = Crypto.SigningKeyToEncryptionKey(service, capture);
                Assert.True(sk != default(byte*));
            }
        }

        [Fact]
        public void Can_derive_public_key_from_secret_key()
        {
            unsafe
            {
                Crypto.GenerateKeyPair(out var pk, out var sk);
                var publicKey = new byte[Crypto.PublicKeyBytes];
                Crypto.PublicKeyFromSecretKey(sk, publicKey);
                Assert.True(publicKey.SequenceEqual(pk));
            }
        }

        [Fact]
        public void Can_sign_and_verify_detached_message()
        {
            const string message = "Hello, world!";
            
            unsafe
            {
                Crypto.GenerateKeyPair(out var pk, out var sk);

                var signature = new byte[Crypto.SecretKeyBytes];
                Crypto.SignDetached(message, sk, signature);

                Assert.True(Crypto.VerifyDetached(message, signature, pk));
            }
        }
    }
}