// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace egregore
{
    internal static class Crypto
    {
        public const uint PublicKeyBytes = 64U;
        public const uint SecretKeyBytes = 32U;

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
                    NativeMethods.crypto_sign_keypair(pk, sk);
                }
            }
        }

        public static string HexString(ReadOnlySpan<byte> bin) => HexString(bin, new Span<byte>(new byte[bin.Length * 2 + 1]));
        public static string HexString(ReadOnlySpan<byte> bin, Span<byte> hex)
        {
            var minLength = bin.Length * 2 + 1;
            if(hex.Length < minLength)
                throw new ArgumentException($"hex buffer is shorter than {minLength}", nameof(hex));

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
    }
}