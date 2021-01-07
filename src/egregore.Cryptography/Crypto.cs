// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace egregore.Cryptography
{
    public static class Crypto
    {
        public const uint PublicKeyBytes = 32U;
        public const uint SecretKeyBytes = 64U;
        public const uint EncryptionKeyBytes = 32U;

        private static int _initialized;

        public static void Initialize()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
                unsafe
                {
                    NativeLibrary.SetDllImportResolver(typeof(Crypto).Assembly, IntegrityCheck.Preload);
                    NativeMethods.sodium_init();
                    NativeMethods.sodium_free(NativeMethods.sodium_malloc(0));
                }
        }

        #region Utilities

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

        public static byte[] ToBinary(this string hexString)
        {
            var buffer = new byte[hexString.Length >> 1];
            var span = buffer.AsSpan();
            ToBinary(hexString, ref span);
            return buffer;
        }

        public static void ToBinary(this string hexString, ref Span<byte> buffer)
        {
            var length = ToBinary(Encoding.UTF8.GetBytes(hexString), buffer);
            if (length < buffer.Length)
                buffer = buffer.Slice(0, length);
        }

        public static int ToBinary(this ReadOnlySpan<byte> hexString, Span<byte> buffer)
        {
            var binMaxLen = buffer.Length;
            var hexLen = hexString.Length;
            unsafe
            {
                fixed (byte* bin = &buffer.GetPinnableReference())
                fixed (byte* hex = &hexString.GetPinnableReference())
                {
                    if (NativeMethods.sodium_hex2bin(bin, binMaxLen, hex, hexLen, null, out var binLen, null) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.sodium_hex2bin));

                    return binLen;
                }
            }
        }

        public static string ToHexString(this ReadOnlySpan<byte> bin)
        {
            return ToHexString(bin, new Span<byte>(new byte[bin.Length * 2 + 1]));
        }

        public static string ToHexString(this ReadOnlySpan<byte> bin, Span<byte> hex)
        {
            var minLength = bin.Length * 2 + 1;
            if (hex.Length < minLength)
                throw new ArgumentOutOfRangeException(nameof(hex), hex.Length,
                    $"Hex buffer is shorter than {minLength}");

            unsafe
            {
                fixed (byte* h = &hex.GetPinnableReference())
                fixed (byte* b = &bin.GetPinnableReference())
                {
                    var ptr = NativeMethods.sodium_bin2hex(h, hex.Length, b, bin.Length);
                    return Marshal.PtrToStringAnsi(ptr);
                }
            }
        }

        public static string Fingerprint(this ReadOnlySpan<byte> publicKey, string value)
        {
            unsafe
            {
                var buffer = new byte[8];

                var app = Encoding.UTF8.GetBytes(value);

                fixed (byte* pk = publicKey)
                fixed (byte* id = buffer)
                fixed (byte* key = app)
                {
                    if (NativeMethods.crypto_generichash(id, buffer.Length, pk, PublicKeyBytes, key, app.Length) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_generichash));
                }

                var fingerprint = ToHexString(buffer);
                return fingerprint;
            }
        }

        #endregion

        #region Cryptographic Hashing

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
                    if (NativeMethods.crypto_hash_sha256(o, i, (ulong) @in.Length) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_hash_sha256));
                }
            }
        }

        #endregion

        #region Public-key Cryptography (Ed25519)

        public static unsafe void GenerateKeyPair(out byte[] publicKey, out byte* secretKey)
        {
            publicKey = new byte[PublicKeyBytes];
            var sk = (byte*) NativeMethods.sodium_malloc(SecretKeyBytes);
            fixed (byte* pk = publicKey)
            {
                if (NativeMethods.crypto_sign_keypair(pk, sk) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_keypair));
                secretKey = sk;
            }
        }

        public static byte[] SigningPublicKeyFromSigningKey(IKeyFileService keyFileService, IKeyCapture capture)
        {
            unsafe
            {
                var sk = keyFileService.GetSecretKeyPointer(capture);
                try
                {
                    var ed25519PublicKey = new byte[PublicKeyBytes];
                    SigningPublicKeyFromSigningKey(sk, ed25519PublicKey);
                    return ed25519PublicKey;
                }
                finally
                {
                    NativeMethods.sodium_free(sk);
                }
            }
        }

        public static unsafe void SigningPublicKeyFromSigningKey(byte* sk, Span<byte> ed25519PublicKey)
        {
            fixed (byte* pk = ed25519PublicKey)
            {
                if (NativeMethods.crypto_sign_ed25519_sk_to_pk(pk, sk) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_pk));
            }
        }

        public static unsafe void EncryptionPublicKeyFromSigningPublicKey(Span<byte> ed25519PublicKey,
            Span<byte> x25519PublicKey)
        {
            fixed (byte* xpk = &x25519PublicKey.GetPinnableReference())
            fixed (byte* epk = &ed25519PublicKey.GetPinnableReference())
            {
                if (NativeMethods.crypto_sign_ed25519_pk_to_curve25519(xpk, epk) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_pk_to_curve25519));
            }
        }

        public static unsafe byte* SigningKeyToEncryptionKey(IKeyFileService keyFileService, IKeyCapture capture)
        {
            var sk = keyFileService.GetSecretKeyPointer(capture);
            try
            {
                return SigningKeyToEncryptionKey(sk);
            }
            finally
            {
                NativeMethods.sodium_free(sk);
            }
        }

        public static unsafe byte* SigningKeyToEncryptionKey(byte* ed25519Sk)
        {
            var x25519Sk = (byte*) NativeMethods.sodium_malloc(SecretKeyBytes);
            if (NativeMethods.crypto_sign_ed25519_sk_to_curve25519(x25519Sk, ed25519Sk) != 0)
                throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_curve25519));
            return x25519Sk;
        }

        public static unsafe ulong SignDetached(string message, byte* sk, Span<byte> signature)
        {
            return SignDetached(Encoding.UTF8.GetBytes(message), sk, signature);
        }

        public static unsafe ulong SignDetached(ReadOnlySpan<byte> message, byte* sk, Span<byte> signature)
        {
            var length = 0UL;

            fixed (byte* sig = &signature.GetPinnableReference())
            fixed (byte* m = &message.GetPinnableReference())
            {
                try
                {
                    if (NativeMethods.crypto_sign_detached(sig, ref length, m, (ulong) message.Length, sk) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_detached));
                }
                finally
                {
                    NativeMethods.sodium_free(sk);
                }
            }

            return length;
        }

        public static bool VerifyDetached(string message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
        {
            return VerifyDetached(Encoding.UTF8.GetBytes(message), signature, publicKey);
        }

        public static bool VerifyDetached(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature,
            ReadOnlySpan<byte> publicKey)
        {
            unsafe
            {
                fixed (byte* sig = &signature.GetPinnableReference())
                fixed (byte* m = &message.GetPinnableReference())
                fixed (byte* pk = &publicKey.GetPinnableReference())
                {
                    var result = NativeMethods.crypto_sign_verify_detached(sig, m, (ulong) message.Length, pk);
                    return result == 0;
                }
            }
        }

        #endregion

        #region File Operations

        public static unsafe byte* LoadSecretKeyPointerFromFileStream(string keyFilePath, FileStream keyFileStream,
            IPersistedKeyCapture capture, [CallerMemberName] string callerMemberName = null)
        {
            if (keyFileStream.CanSeek)
                keyFileStream.Seek(0, SeekOrigin.Begin);
            if (!KeyFileManager.TryLoadKeyFile(keyFileStream, Console.Out, Console.Error, out var sk, capture))
                throw new InvalidOperationException(
                    $"{callerMemberName}: Cannot load key file at path '{keyFilePath}'");
            return sk;
        }

        public static unsafe byte* LoadSecretKeyPointerFromFileStream(string keyFilePath, FileStream keyFileStream,
            IKeyCapture capture, [CallerMemberName] string callerMemberName = null)
        {
            if (keyFileStream.CanSeek)
                keyFileStream.Seek(0, SeekOrigin.Begin);
            if (!KeyFileManager.TryLoadKeyFile(keyFileStream, Console.Out, Console.Error, out var sk, capture))
                throw new InvalidOperationException(
                    $"{callerMemberName}: Cannot load key file at path '{keyFilePath}'");
            return sk;
        }

        #endregion

        #region Helper Functions

        #endregion
    }
}