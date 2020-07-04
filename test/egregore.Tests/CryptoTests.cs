// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace egregore.Tests
{
    public class CryptoTests
    {
        [Fact]
        public void Can_use_guarded_heap_for_secret_key_from_file()
        {
            var publicKey = GenerateSecretKeyOnDisk(out var fileName);

            unsafe
            {
                using var fs = File.OpenRead(fileName);
                var sk = Crypto.OpenGuardedHeap(fs, (int) Crypto.SecretKeyBytes);
                try
                {
                    // use the guarded heap in a crypto operation (get pk from sk)
                    fixed (byte* pk = &new Span<byte>(new byte[Crypto.PublicKeyBytes]).GetPinnableReference())
                    {
                        if(NativeMethods.crypto_sign_ed25519_sk_to_pk(pk, sk) != 0)
                            throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_pk));
                    }

                    // check that the pk is valid
                    Assert.True(publicKey.SequenceEqual(publicKey.ToArray()));
                }
                finally
                {
                    Crypto.CloseGuardedHeap(sk);
                }
            }
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
            var (pk, sk) = Crypto.GenerateKeyPairDangerous();
            Assert.NotEmpty(pk);
            Assert.NotEmpty(sk);
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
            GenerateSecretKeyOnDisk(out var fileName);
            Crypto.SigningKeyToEncryptionKeyDangerous(File.OpenRead(fileName));
        }

        [Fact]
        public void Can_derive_public_key_from_secret_key()
        {
            var (pk, sk) = Crypto.GenerateKeyPairDangerous();
            var publicKey = Crypto.PublicKeyFromSecretKeyDangerous(sk);
            Assert.True(publicKey.SequenceEqual(pk));
        }

        internal static byte[] GenerateSecretKeyOnDisk(out string fileName)
        {
            // simulate a valid private key on disk
            var (pk, sk) = Crypto.GenerateKeyPairDangerous();
            fileName = Path.GetTempFileName();
            File.WriteAllBytes(fileName, sk);
            return pk;
        }
    }
}