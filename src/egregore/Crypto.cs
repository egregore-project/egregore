// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace egregore
{
    internal static class Crypto
    {
        public const uint PublicKeyBytes = 32U;
        public const uint SecretKeyBytes = 64U;

        static Crypto()
        {
            NativeLibrary.SetDllImportResolver(typeof(Crypto).Assembly, Minisign.VerifyImportResolver);
        }

        public static byte[] Nonce(uint size)
        {
            var buffer = new byte[size];
            FillNonZeroBytes(buffer, size);
            return buffer;
        }

        public static void FillNonZeroBytes(Span<byte> buffer)
        {
            unsafe
            {
                fixed (byte* b = &buffer.GetPinnableReference())
                {
                    NativeMethods.randombytes_buf(b, (uint) buffer.Length);
                }
            }
        }

        public static void FillNonZeroBytes(Span<byte> buffer, uint size)
        {
            unsafe
            {
                fixed (byte* b = &buffer.GetPinnableReference())
                {
                    NativeMethods.randombytes_buf(b, size);
                }
            }
        }

        public static (byte[] publicKey, byte[] secretKey) GenerateKeyPair()
        {
            var pk = new byte[PublicKeyBytes];
            var sk = new byte[SecretKeyBytes];
            GenerateKeyPair(pk, sk);
            return (pk, sk);
        }

        public static void GenerateKeyPair(Span<byte> publicKey, Span<byte> secretKey)
        {
            unsafe
            {
                fixed (byte* pk = &publicKey.GetPinnableReference())
                fixed (byte* sk = &secretKey.GetPinnableReference())
                {
                    if(NativeMethods.crypto_sign_keypair(pk, sk) != 0)
                        throw new InvalidOperationException();
                }
            }
        }

        public static string HexString(ReadOnlySpan<byte> bin) => HexString(bin, new Span<byte>(new byte[bin.Length * 2 + 1]));
        public static string HexString(ReadOnlySpan<byte> bin, Span<byte> hex)
        {
            var minLength = bin.Length * 2 + 1;
            if(hex.Length < minLength)
                throw new ArgumentOutOfRangeException(nameof(hex), hex.Length, $"hex buffer is shorter than {minLength}");

            unsafe
            {
                fixed(byte* h = &hex.GetPinnableReference())
                fixed(byte* b = &bin.GetPinnableReference())
                {
                    var ptr = NativeMethods.sodium_bin2hex(h, hex.Length, b, bin.Length);
                    return Marshal.PtrToStringAnsi(ptr);
                }
            }
        }

        public static byte[] Sha256(this ReadOnlySpan<byte> @in)
        {
            var @out = new byte[32U];
            @in.Sha256(@out);
            return @out;
        }

        public static void Sha256(this ReadOnlySpan<byte> @in, Span<byte> @out)
        { 
            unsafe
            {
                fixed (byte* o = &@out.GetPinnableReference())
                fixed (byte* i = &@in.GetPinnableReference())
                {
                    if(NativeMethods.crypto_hash_sha256(o, i, (ulong) @in.Length) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_hash_sha256));
                }
            }
        }

        public static byte[] PublicKeyFromSecretKey(ReadOnlySpan<byte> ed25519SecretKey)
        {
            var ed25519PublicKey = new byte[PublicKeyBytes];
            PublicKeyFromSecretKey(ed25519SecretKey, ed25519PublicKey);
            return ed25519PublicKey;
        }

        public static void PublicKeyFromSecretKey(ReadOnlySpan<byte> ed25519SecretKey, Span<byte> ed25519PublicKey)
        {
            unsafe
            {
                fixed(byte* sk = &ed25519SecretKey.GetPinnableReference())
                fixed(byte* pk = &ed25519PublicKey.GetPinnableReference())
                {
                    if (NativeMethods.crypto_sign_ed25519_sk_to_pk(pk, sk) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_pk));
                }
            }
        }

        public static byte[] SigningKeyToEncryptionKey(ReadOnlySpan<byte> ed25519SecretKey)
        {
            var curve25519Sk = new byte[SecretKeyBytes];
            SigningKeyToEncryptionKey(ed25519SecretKey, curve25519Sk);
            return curve25519Sk;
        }

        public static void SigningKeyToEncryptionKey(ReadOnlySpan<byte> ed25519SecretKey, Span<byte> x25519Sk)
        {
            unsafe
            {
                fixed(byte* e = &ed25519SecretKey.GetPinnableReference())
                fixed(byte* x = &x25519Sk.GetPinnableReference())
                {
                    if (NativeMethods.crypto_sign_ed25519_sk_to_curve25519(x, e) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_curve25519));
                }
            }
        }
    }
}