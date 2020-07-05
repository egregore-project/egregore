// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using egregore.Ontology;

namespace egregore
{
    internal static class Crypto
    {
        public const uint PublicKeyBytes = 32U;
        public const uint SecretKeyBytes = 64U;

        static Crypto()
        {
            if (NativeMethods.sodium_init() != 0)
                throw new InvalidOperationException(nameof(NativeMethods.sodium_init));

            NativeLibrary.SetDllImportResolver(typeof(Crypto).Assembly, Minisign.VerifyImportResolver);
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
                    if(NativeMethods.sodium_hex2bin(bin, binMaxLen, hex, hexLen, null, out var binLen, null) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.sodium_hex2bin));

                    return binLen;
                }
            }
        }

        public static string ToHexString(this ReadOnlySpan<byte> bin) => ToHexString(bin, new Span<byte>(new byte[bin.Length * 2 + 1]));
        public static string ToHexString(this ReadOnlySpan<byte> bin, Span<byte> hex)
        {
            var minLength = bin.Length * 2 + 1;
            if(hex.Length < minLength)
                throw new ArgumentOutOfRangeException(nameof(hex), hex.Length, $"Hex buffer is shorter than {minLength}");

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
                    if(NativeMethods.crypto_hash_sha256(o, i, (ulong) @in.Length) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_hash_sha256));
                }
            }
        }

        #endregion

        #region Public-key cryptography (Ed25519)

        public static unsafe void GenerateKeyPair(out byte[] publicKey, out byte* secretKey)
        {
            publicKey = new byte[PublicKeyBytes];
            var sk = (byte*) NativeMethods.sodium_malloc(SecretKeyBytes);
            fixed (byte* pk = publicKey)
            {
                if(NativeMethods.crypto_sign_keypair(pk, sk) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_keypair));
                
                secretKey = sk;
            }
        }

        /// <summary>
        /// Generate a new key pair for offline purposes. NEVER use the secret key outside of tests. 
        /// </summary>
        public static void GenerateKeyPairDangerous(Span<byte> publicKey, Span<byte> secretKey)
        {
            unsafe
            {
                fixed (byte* pk = &publicKey.GetPinnableReference())
                fixed (byte* sk = &secretKey.GetPinnableReference())
                {
                    if(NativeMethods.crypto_sign_keypair(pk, sk) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_keypair));
                }
            }
        }

        public static byte[] PublicKeyFromSecretKeyDangerous(ReadOnlySpan<byte> ed25519SecretKey)
        {
            var ed25519PublicKey = new byte[PublicKeyBytes];
            unsafe
            {
                fixed(byte* sk = &ed25519SecretKey.GetPinnableReference())
                fixed(byte* pk = &ed25519PublicKey.AsSpan().GetPinnableReference())
                {
                    if (NativeMethods.crypto_sign_ed25519_sk_to_pk(pk, sk) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_pk));
                }
            }
            return ed25519PublicKey;
        }

        public static byte[] PublicKeyFromSecretKey(IKeyFileService keyFileService, IKeyCapture capture = null)
        {
            unsafe
            {
                var sk = GetSecretKeyPointer(keyFileService, capture);
                var ed25519PublicKey = new byte[PublicKeyBytes];
                PublicKeyFromSecretKey(sk, ed25519PublicKey);
                return ed25519PublicKey;
            }
        }

        internal static unsafe byte* GetSecretKeyPointer(IKeyFileService keyFileService, IKeyCapture capture = null, [CallerMemberName] string callerMemberName = null)
        {
            var fs = keyFileService.GetKeyFileStream();
            if (fs.CanSeek)
                fs.Seek(0, SeekOrigin.Begin);

            if (!PasswordStorage.TryLoadKeyFile(fs, Console.Out, Console.Error, out var sk, capture ?? Constants.ConsoleKeyCapture))
                throw new InvalidOperationException($"{callerMemberName}: Cannot load key file at path '{keyFileService.GetKeyFilePath()}'");
            return sk;
        }

        public static unsafe void PublicKeyFromSecretKey(byte* sk, Span<byte> ed25519PublicKey)
        {
            try
            {
                fixed(byte* pk = &ed25519PublicKey.GetPinnableReference())
                {
                    if (NativeMethods.crypto_sign_ed25519_sk_to_pk(pk, sk) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_pk));
                }
            }
            finally
            {
                NativeMethods.sodium_free(sk);
            }
        }

        public static unsafe byte* SigningKeyToEncryptionKey(IKeyFileService keyFileService, IKeyCapture capture = null) => SigningKeyToEncryptionKey(GetSecretKeyPointer(keyFileService, capture));
        public static unsafe byte* SigningKeyToEncryptionKey(byte* ed25519Sk)
        {
            try
            {
                var x25519Sk = (byte*) NativeMethods.sodium_malloc(SecretKeyBytes);
                if (NativeMethods.crypto_sign_ed25519_sk_to_curve25519(x25519Sk, ed25519Sk) != 0)
                    throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_curve25519));
                return x25519Sk;
            }
            finally
            {
                NativeMethods.sodium_free(ed25519Sk);
            }
        }

        public static unsafe ulong SignDetached(string message, byte* sk, Span<byte> signature) => SignDetached(Encoding.UTF8.GetBytes(message), sk, signature);
        public static unsafe ulong SignDetached(ReadOnlySpan<byte> message, byte* sk, Span<byte> signature)
        {
            var length = 0UL;

            fixed (byte* sig = &signature.GetPinnableReference())
            fixed (byte* m = &message.GetPinnableReference())
            {
                try
                {
                    if(NativeMethods.crypto_sign_detached(sig, ref length, m, (ulong) message.Length, sk) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_detached));
                }
                finally
                {
                    NativeMethods.sodium_free(sk);
                }
            }

            return length;
        }

        public static bool VerifyDetached(string message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey) => VerifyDetached(Encoding.UTF8.GetBytes(message), signature, publicKey);
        public static bool VerifyDetached(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
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
    }
}