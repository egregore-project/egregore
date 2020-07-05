// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Encryption based on process followed in Frank Denis' minisign:
// https://github.com/jedisct1/minisign/blob/master/LICENSE
/*
 * Copyright (c) 2015-2020
 * Frank Denis <j at pureftpd dot org>
 *
 * Permission to use, copy, modify, and/or distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace egregore
{
    /// <summary>
    /// Encrypts secret keys by deriving another key from a password and mixing.
    /// This is based largely on Frank Denis' minisign: https://github.com/jedisct1/minisign/blob/master/src/minisign.c although the file format is NOT compatible.
    /// <remarks>Checksum function currently uses SCrypt, but if compatibility with Minisign and other tools is not a goal, then Argon2 is a better choice.</remarks>
    /// </summary>
    internal static class PasswordStorage
    {
        private static readonly string BackspaceSpaceBackspace = "\b \b";

        public const byte SigAlgBytes = 2;
        public const byte KdfAlgBytes = 2;
        public const byte ChkAlgBytes = 2;

        public static readonly byte[] SigAlg = {(byte) 'E', (byte) 'd'};
        public static readonly byte[] KdfAlg = {(byte) 'S', (byte) 'c'};
        public static readonly byte[] ChkAlg = {(byte) 'B', (byte) '2'};

        public const byte KeyNumBytes = 8;
        public const uint KdfSaltBytes = 32U;
        public const int ChecksumBytes = 64;

        public const int KdfOpsLimitBytes = sizeof(ulong);
        public const ulong KdfOpsLimit = 33554432UL;

        public const int KdfMemLimitBytes = sizeof(int);
        public const int KdfMemLimit = 1073741824;
        
        public const uint CipherBytes = KeyNumBytes + Crypto.SecretKeyBytes + ChecksumBytes;
        public const ulong ChecksumInputBytes = SigAlgBytes + KeyNumBytes + Crypto.SecretKeyBytes;

        public const long KeyFileBytes = SigAlgBytes + KdfAlgBytes + ChkAlgBytes + KeyNumBytes + KdfSaltBytes + KdfOpsLimitBytes + KdfMemLimitBytes + CipherBytes + ChecksumBytes;
        
        public static unsafe bool TryCapturePassword(string instructions, IKeyCapture @in, TextWriter @out, TextWriter error, out byte* password, out int passwordLength)
        {
            const int passwordMaxBytes = 1024;
            password = (byte*) NativeMethods.sodium_malloc(passwordMaxBytes);
            var passwordConfirm = NativeMethods.sodium_malloc(passwordMaxBytes);
            passwordLength = 0;

            try
            {
                @out.WriteLine(instructions);

                var initPwdLength = 0;
                using var initPwd = new UnmanagedMemoryStream(password, 0, passwordMaxBytes, FileAccess.Write);
                @out.Write(Strings.PasswordPrompt);
                ConsoleKeyInfo key;
                do
                {
                    key = @in.ReadKey();
                    if (key.Key == ConsoleKey.Enter)
                        break;
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (initPwdLength > 0)
                        {
                            initPwd.Position--;
                            initPwd.WriteByte(0);
                            initPwd.Position--;
                            initPwdLength--;
                            Console.Write(BackspaceSpaceBackspace);
                        }
                        continue;
                    }
                    initPwd.WriteByte((byte) key.KeyChar);
                    key = default;
                    @out.Write(Strings.PasswordMask);
                    initPwdLength++;
                } while (key.Key != ConsoleKey.Enter && initPwdLength < passwordMaxBytes);

                var confirmPwdLength = 0;
                using var confirmPwd = new UnmanagedMemoryStream((byte*) passwordConfirm, 0, passwordMaxBytes, FileAccess.Write);
                
                @out.WriteLine();
                @out.Write(Strings.ConfirmPasswordPrompt);
                do
                {
                    key = @in.ReadKey();
                    if (key.Key == ConsoleKey.Enter)
                        break;
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (confirmPwdLength > 0)
                        {
                            confirmPwd.Position--;
                            confirmPwd.WriteByte(0);
                            confirmPwd.Position--;
                            confirmPwdLength--;
                            Console.Write(BackspaceSpaceBackspace);
                        }
                        continue;
                    }
                    confirmPwd.WriteByte((byte) key.KeyChar);
                    key = default;
                    @out.Write(Strings.PasswordMask);
                    confirmPwdLength++;
                } while (key.Key != ConsoleKey.Enter && confirmPwdLength < passwordMaxBytes);

                @out.WriteLine();

                passwordLength = Math.Max(initPwdLength, confirmPwdLength);
                if (passwordLength == 0)
                {
                    passwordLength = -1;
                    error.WriteLine(Strings.InvalidPasswordLength);
                    NativeMethods.sodium_free(password);
                    password = default;
                    return false;
                }

                if (initPwdLength != confirmPwdLength)
                {
                    passwordLength = -1;
                    error.WriteLine(Strings.PasswordMismatch);
                    NativeMethods.sodium_free(password);
                    password = default;
                    return false;
                }

                if (NativeMethods.sodium_memcmp(password, passwordConfirm, passwordLength) != 0)
                {
                    error.WriteLine(Strings.PasswordMismatch);
                    NativeMethods.sodium_free(password);
                    password = default;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                error.WriteLine();
                NativeMethods.sodium_free(password);
                password = default;
                return false;
            }
            finally
            {
                NativeMethods.sodium_free(passwordConfirm);
            }

            return true;
        }

        public static unsafe byte* Xor(byte* dst, byte* src, uint len)
        {
            var xor = (byte*) NativeMethods.sodium_malloc(len);
            for (var i = 0U; i < len; i++)
            {
                var a = dst[i];
                var b = src[i];
                xor[i] = (byte) (a ^ b);
            }
            return xor;
        }

        public static unsafe bool TryGenerateKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error, IKeyCapture keyCapture = null)
        {
            keyCapture ??= Constants.ConsoleKeyCapture;

            if (!TryCapturePassword(Strings.GenerateKeyInstructions, keyCapture, @out, error, out var password, out var passwordLength))
                return false;

            var sk = (byte*) NativeMethods.sodium_malloc(Crypto.SecretKeyBytes);
            try
            {
                var pk = (byte*) NativeMethods.sodium_malloc(Crypto.PublicKeyBytes);

                try
                {
                    // Create a new signing key pair:
                    if (NativeMethods.crypto_sign_keypair(pk, sk) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_sign_keypair));
                }
                finally
                {
                    NativeMethods.sodium_free(pk);
                }

                // Create key number to mix with the checksum:
                var keyNumber = (byte*) NativeMethods.sodium_malloc(KeyNumBytes);

                // Checksum = Blake2B(SigAlg || KeyNumber || SecretKey):
                var offset = 0;
                var checksumInput = (byte*) NativeMethods.sodium_malloc(ChecksumInputBytes);
                var checksum = (byte*) NativeMethods.sodium_malloc(ChecksumBytes);
                try
                {
                    NativeMethods.randombytes_buf(keyNumber, KeyNumBytes);

                    fixed (byte* src = SigAlg)
                        for (var i = 0; i < SigAlgBytes; i++)
                            checksumInput[offset++] = src[i];
                    for (var i = 0; i < KeyNumBytes; i++)
                        checksumInput[offset++] = keyNumber[i];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        checksumInput[offset++] = sk[i];

                    if (NativeMethods.crypto_generichash(checksum, ChecksumBytes, checksumInput, ChecksumInputBytes, null, 0) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_generichash));
                }
                finally
                {
                    NativeMethods.sodium_free(checksumInput);
                }

                //
                // Prepare cipher block for encryption: (KeyNum || SecretKey || Checksum)
                var cipher = (byte*) NativeMethods.sodium_malloc(CipherBytes);
                try
                {
                    offset = 0;
                    for (var i = 0; i < KeyNumBytes; i++)
                        cipher[offset++] = keyNumber[i];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        cipher[offset++] = sk[i];
                    for (var i = 0; i < ChecksumBytes; i++)
                        cipher[offset++] = checksum[i];
                }
                finally
                {
                    NativeMethods.sodium_free(sk);
                }
                
                @out.Write(Strings.EncryptionInProgressMessage);

                //
                // Encrypt the secret key by mixing it with another key derived from the password:
                // SecretKey ^ crypto_pwhash_scryptsalsa208sha256(password || salt || opsLimit || memLimit)):
                const ulong opsLimit = KdfOpsLimit;
                const int memLimit = KdfMemLimit;
                var kdfSalt = (byte*) NativeMethods.sodium_malloc(KdfSaltBytes);
                var stream = (byte*) NativeMethods.sodium_malloc(CipherBytes);
                byte* xor;
                try
                {
                    NativeMethods.randombytes_buf(kdfSalt, KdfSaltBytes);

                    if (NativeMethods.crypto_pwhash_scryptsalsa208sha256(stream, CipherBytes, password, (ulong) passwordLength, kdfSalt, opsLimit, memLimit) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_pwhash_scryptsalsa208sha256));

                    xor = Xor(cipher, stream, CipherBytes);
                }
                finally
                {
                    NativeMethods.sodium_free(password);
                    NativeMethods.sodium_free(cipher);
                    NativeMethods.sodium_free(stream);
                }
                @out.WriteLine();

                // 
                // Write key file: (SigAlg || KdfAlg || ChkAlg || KeyNum || KdfSalt || OpsLimit || MemLimit || Cipher || Checksum)
                var file = (byte*) NativeMethods.sodium_malloc(KeyFileBytes);
                try
                {
                    offset = 0;

                    fixed (byte* src = SigAlg)
                        for (var i = 0; i < SigAlgBytes; i++)
                            file[offset++] = src[i];
                    fixed (byte* src = KdfAlg)
                        for (var i = 0; i < KdfAlgBytes; i++)
                            file[offset++] = src[i];
                    fixed (byte* src = ChkAlg)
                        for (var i = 0; i < ChkAlgBytes; i++)
                            file[offset++] = src[i];

                    for (var i = 0; i < KeyNumBytes; i++)
                        file[offset++] = keyNumber[i];

                    for (var i = 0; i < KdfSaltBytes; i++)
                        file[offset++] = kdfSalt[i];

                    var ops = BitConverter.GetBytes(opsLimit);
                    foreach (var b in ops)
                        file[offset++] = b;

                    var mem = BitConverter.GetBytes(memLimit);
                    foreach (var b in mem)
                        file[offset++] = b;
                    
                    for (var i = 0; i < CipherBytes; i++)
                        file[offset++] = xor[i];
                    
                    for (var i = 0; i < ChecksumBytes; i++)
                        file[offset++] = checksum[i];

                    if (offset != KeyFileBytes)
                    {
                        error.WriteLine(Strings.InvalidKeyFileBuffer);
                        NativeMethods.sodium_free(file);
                        return false;
                    }
                }
                finally
                {
                    NativeMethods.sodium_free(keyNumber);
                    NativeMethods.sodium_free(kdfSalt);
                    NativeMethods.sodium_free(checksum);
                    NativeMethods.sodium_free(xor);
                }

                try
                {
                    keyFileStream.Seek(0, SeekOrigin.Begin);
                    for (var i = 0; i < KeyFileBytes; i++)
                        keyFileStream.WriteByte(0);

                    using var mmf = MemoryMappedFile.CreateFromFile(keyFileStream, null, KeyFileBytes, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
                    using var uvs = mmf.CreateViewStream(0, KeyFileBytes, MemoryMappedFileAccess.ReadWrite);
                    for (var i = 0; i < (int) KeyFileBytes; i++)
                        uvs.WriteByte(file[i]);
                    keyFileStream.Flush();
                }
                finally
                {
                    NativeMethods.sodium_free(file);
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                error.WriteLine(Strings.KeyFileGenerateFailure);
                return false;
            }
        }

        public static unsafe bool TryLoadKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error, out byte* secretKey, IKeyCapture keyCapture = null)
        {
            keyCapture ??= Constants.ConsoleKeyCapture;

            secretKey = default;

            if (!TryCapturePassword(Strings.LoadKeyInstructions, keyCapture, @out, error, out var password, out var passwordLength))
                return false;
            
            // 
            // Read key file: (SigAlg || KdfAlg || ChkAlg || KeyNum || KdfSalt || OpsLimit || MemLimit || Cipher || Checksum)
            try
            {
                using var mmf = MemoryMappedFile.CreateFromFile(keyFileStream, null, KeyFileBytes, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                using var uvs = mmf.CreateViewStream(0, KeyFileBytes, MemoryMappedFileAccess.Read);

                var sigAlg = NativeMethods.sodium_malloc(SigAlgBytes);
                try
                {
                    var buffer = (byte*) sigAlg;
                    for (var i = 0; i < SigAlgBytes; i++)
                        buffer[i] = (byte) uvs.ReadByte();

                    fixed (void* src = SigAlg)
                        if (NativeMethods.sodium_memcmp(sigAlg, src, SigAlg.Length) != 0)
                        {
                            error.WriteLine(Strings.InvalidSignatureAlgorithm);
                            return false;
                        }
                }
                finally
                {
                    NativeMethods.sodium_free(sigAlg); // since we only have one, we can toss this
                }

                var kdfAlg = NativeMethods.sodium_malloc(KdfAlgBytes);
                try
                {
                    var buffer = (byte*) kdfAlg;
                    for (var i = 0; i < KdfAlgBytes; i++)
                        buffer[i] = (byte) uvs.ReadByte();

                    fixed (void* src = KdfAlg)
                        if (NativeMethods.sodium_memcmp(kdfAlg, src, KdfAlgBytes) != 0)
                        {
                            error.WriteLine(Strings.InvalidKeyDerivationFunction);
                            return false;
                        }
                }
                finally
                {
                    NativeMethods.sodium_free(kdfAlg); // since we only have one, we can toss this
                }

                var chkAlg = NativeMethods.sodium_malloc(ChkAlgBytes);
                try
                {
                    var buffer = (byte*) chkAlg;
                    for (var i = 0; i < ChkAlgBytes; i++)
                        buffer[i] = (byte) uvs.ReadByte();

                    fixed (void* src = ChkAlg)
                        if (NativeMethods.sodium_memcmp(chkAlg, src, ChkAlgBytes) != 0)
                        {
                            error.WriteLine(Strings.InvalidChecksumFunction);
                            return false;
                        }
                }
                finally
                {
                    NativeMethods.sodium_free(chkAlg); // since we only have one, we can toss this
                }

                var fileKeyNumber = (byte*) NativeMethods.sodium_malloc(KeyNumBytes);
                for (var i = 0; i < KeyNumBytes; i++)
                    fileKeyNumber[i] = (byte) uvs.ReadByte();

                var kdfSalt = (byte*) NativeMethods.sodium_malloc(KdfSaltBytes);
                for (var i = 0; i < KdfSaltBytes; i++)
                    kdfSalt[i] = (byte) uvs.ReadByte();
                
                var opsLimitData = (byte*) NativeMethods.sodium_malloc(KdfOpsLimitBytes);
                for (var i = 0; i < KdfOpsLimitBytes; i++)
                    opsLimitData[i] = (byte) uvs.ReadByte();
                var opsLimit = BitConverter.ToUInt64(new ReadOnlySpan<byte>(opsLimitData, KdfOpsLimitBytes));
                NativeMethods.sodium_free(opsLimitData);

                var memLimitData = (byte*) NativeMethods.sodium_malloc(KdfMemLimitBytes);
                for (var i = 0; i < KdfMemLimitBytes; i++)
                    memLimitData[i] = (byte) uvs.ReadByte();
                var memLimit = BitConverter.ToInt32(new ReadOnlySpan<byte>(memLimitData, KdfMemLimitBytes));
                NativeMethods.sodium_free(memLimitData);

                var fileCipher = (byte*) NativeMethods.sodium_malloc(CipherBytes);
                for (var i = 0; i < CipherBytes; i++)
                    fileCipher[i] = (byte) uvs.ReadByte();

                var fileChecksum = (byte*) NativeMethods.sodium_malloc(ChecksumBytes);
                for (var i = 0; i < ChecksumBytes; i++)
                    fileChecksum[i] = (byte) uvs.ReadByte();

                var eof = uvs.ReadByte();
                if (eof != -1)
                {
                    error.WriteLine();
                    return false;
                }

                @out.Write(Strings.DecryptionInProgressMessage);

                var stream = (byte*) NativeMethods.sodium_malloc(CipherBytes);
                byte* xor;
                try
                {
                    if (NativeMethods.crypto_pwhash_scryptsalsa208sha256(stream, CipherBytes, password, (ulong) passwordLength, kdfSalt, opsLimit, memLimit) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_pwhash_scryptsalsa208sha256));

                    xor = Xor(fileCipher, stream, CipherBytes);
                }
                finally
                {
                    NativeMethods.sodium_free(password);
                    NativeMethods.sodium_free(kdfSalt);
                    NativeMethods.sodium_free(fileCipher);
                    NativeMethods.sodium_free(stream);
                }

                @out.WriteLine(Strings.DecryptionCompleteMessage);

                //
                // Deconstruct cipher block for checksum: (KeyNum || SecretKey || Checksum)
                var fileCipherKeyNumber = (byte*) NativeMethods.sodium_malloc(KeyNumBytes);
                var sk = (byte*) NativeMethods.sodium_malloc(Crypto.SecretKeyBytes);
                var fileCipherChecksum = (byte*) NativeMethods.sodium_malloc(ChecksumBytes);
                var offset = 0;
                try
                {
                    for (var i = 0; i < KeyNumBytes; i++)
                        fileCipherKeyNumber[i] = xor[offset++];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        sk[i] = xor[offset++];
                    for (var i = 0; i < ChecksumBytes; i++)
                        fileCipherChecksum[i] = xor[offset++];

                    if (NativeMethods.sodium_memcmp(fileCipherChecksum, fileChecksum, ChecksumBytes) != 0)
                    {
                        NativeMethods.sodium_free(sk);
                        error.WriteLine(Strings.InvalidDecryptionPassword);
                        return false;
                    }

                    if (NativeMethods.sodium_memcmp(fileCipherKeyNumber, fileKeyNumber, KeyNumBytes) != 0)
                    {
                        NativeMethods.sodium_free(sk);
                        error.WriteLine(Strings.InvalidKeyFileKeyNumber);
                        return false;
                    }
                }
                finally
                {
                    NativeMethods.sodium_free(fileKeyNumber);
                    NativeMethods.sodium_free(fileChecksum);
                    NativeMethods.sodium_free(xor);
                }

                //
                // Checksum = Blake2B(SigAlg || KeyNumber || SecretKey):
                offset = 0;
                var checksumInput = (byte*) NativeMethods.sodium_malloc(ChecksumInputBytes);
                var checksum = (byte*) NativeMethods.sodium_malloc(ChecksumBytes);
                try
                {
                    fixed (byte* src = SigAlg)
                        for (var i = 0; i < SigAlgBytes; i++)
                            checksumInput[offset++] = src[i];
                    for (var i = 0; i < KeyNumBytes; i++)
                        checksumInput[offset++] = fileCipherKeyNumber[i];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        checksumInput[offset++] = sk[i];

                    if (NativeMethods.crypto_generichash(checksum, ChecksumBytes, checksumInput, ChecksumInputBytes, null, 0) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_generichash));

                    if (NativeMethods.sodium_memcmp(checksum, fileCipherChecksum, ChecksumBytes) != 0)
                    {
                        NativeMethods.sodium_free(sk);
                        error.WriteLine(Strings.InvalidKeyFileChecksum);
                        return false;
                    }
                }
                finally
                {
                    NativeMethods.sodium_free(fileCipherKeyNumber);
                    NativeMethods.sodium_free(fileCipherChecksum);
                    NativeMethods.sodium_free(checksum);
                    NativeMethods.sodium_free(checksumInput);
                }

                secretKey = sk;
                return true;
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
                error.WriteLine(Strings.KeyFileLoadFailure);
                return false;
            }
        }
    }
}