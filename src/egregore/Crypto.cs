// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

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

        /// <summary>
        /// Generate a new key pair for offline purposes only.
        /// <remarks>
        /// NEVER use the secret key outside of tests. 
        /// </remarks>
        /// </summary>
        public static (byte[] publicKey, byte[] secretKey) GenerateKeyPairDangerous()
        {
            var pk = new byte[PublicKeyBytes];
            var sk = new byte[SecretKeyBytes];
            GenerateKeyPairDangerous(pk, sk);
            return (pk, sk);
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
                        throw new InvalidOperationException();
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

        public static byte[] PublicKeyFromSecretKey(FileStream fs)
        {
            var ed25519PublicKey = new byte[PublicKeyBytes];
            PublicKeyFromSecretKey(fs, ed25519PublicKey);
            return ed25519PublicKey;
        }

        public static void PublicKeyFromSecretKey(FileStream fs, Span<byte> ed25519PublicKey)
        {
            unsafe
            {
                var sk = OpenGuardedHeap(fs, (int) SecretKeyBytes);
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
                    CloseGuardedHeap(sk);
                }
            }
        }

        /// <summary>
        /// <remarks>NEVER use this outside of tests.</remarks>
        /// </summary>
        public static byte[] SigningKeyToEncryptionKeyDangerous(FileStream fs)
        {
            var curve25519Sk = new byte[SecretKeyBytes];
            SigningKeyToEncryptionKeyDangerous(fs, curve25519Sk);
            return curve25519Sk;
        }

        /// <summary>
        /// <remarks>NEVER use this outside of tests.</remarks>
        /// </summary>
        public static void SigningKeyToEncryptionKeyDangerous(FileStream fs, Span<byte> x25519Sk)
        {
            unsafe
            {
                var e = OpenGuardedHeap(fs, (int) SecretKeyBytes);
                try
                {
                    fixed (byte* x = &x25519Sk.GetPinnableReference())
                    {
                        if (NativeMethods.crypto_sign_ed25519_sk_to_curve25519(x, e) != 0)
                            throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_ed25519_sk_to_curve25519));
                    }
                }
                finally
                {
                    CloseGuardedHeap(e);
                }
            }
        }

        public static ulong SignDetached(string message, FileStream fs, Span<byte> signature) => SignDetached(Encoding.UTF8.GetBytes(message), fs, signature);
        public static ulong SignDetached(ReadOnlySpan<byte> message, FileStream fs, Span<byte> signature)
        {
            var length = 0UL;

            unsafe
            {
                fixed (byte* sig = &signature.GetPinnableReference())
                fixed (byte* m = &message.GetPinnableReference())
                {
                    var sk = OpenGuardedHeap(fs, (int) SecretKeyBytes);
                    try
                    {
                        if(NativeMethods.crypto_sign_detached(sig, ref length, m, (ulong) message.Length, sk) != 0)
                            throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_detached));
                    }
                    finally
                    {
                        CloseGuardedHeap(sk);
                    }
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
                    return NativeMethods.crypto_sign_verify_detached(sig, m, (ulong) message.Length, pk) == 0;
                }
            }
        }

        #endregion

        #region Guarded Heap 

        public static unsafe byte* OpenGuardedHeap(FileStream fs, uint size) 
        {
            var ptr = (byte*) NativeMethods.sodium_malloc(size);
            try
            {
                // open an unmanaged view stream
                using var mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
                using var uvs = mmf.CreateViewStream(0, size, MemoryMappedFileAccess.Read);
                
                // get a writable stream to the guarded heap
                using var ums = new UnmanagedMemoryStream(ptr, size, size, FileAccess.Write);
                for (var i = 0; i < size; i++)
                {
                    var @byte = (byte) uvs.ReadByte();
                    ums.WriteByte(@byte);
                }

                return ptr;
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
                NativeMethods.sodium_free(ptr);
                throw;
            }
        }

        public static unsafe void CloseGuardedHeap(byte* sk) => NativeMethods.sodium_free(sk);

        #endregion
    }
}