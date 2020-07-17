// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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
using egregore.Extensions;

namespace egregore.IO
{
    /// <summary>
    ///     Encrypts secret keys by deriving another key from a password and mixing.
    ///     This is based largely on Frank Denis' minisign: https://github.com/jedisct1/minisign/blob/master/src/minisign.c
    ///     although the file format is NOT compatible.
    ///     <remarks>
    ///         Checksum function currently uses SCrypt, but if compatibility with Minisign and other tools is not a goal,
    ///         then Argon2 is a better choice.
    ///     </remarks>
    /// </summary>
    internal static class KeyFileManager
    {
        private const string BackspaceSpaceBackspace = "\b \b";

        public const byte SigAlgBytes = 2;
        public const byte KdfAlgBytes = 2;
        public const byte ChkAlgBytes = 2;

        public const byte KeyNumBytes = 8;
        public const uint KdfSaltBytes = 32U;
        public const int ChecksumBytes = 64;

        public const int KdfOpsLimitBytes = sizeof(ulong);
        public const ulong KdfOpsLimit = 33554432UL;

        public const int KdfMemLimitBytes = sizeof(int);
        public const int KdfMemLimit = 1073741824;

        public const uint CipherBytes = KeyNumBytes + Crypto.SecretKeyBytes + ChecksumBytes;
        public const ulong ChecksumInputBytes = SigAlgBytes + KeyNumBytes + Crypto.SecretKeyBytes;

        public const long KeyFileBytes = SigAlgBytes + KdfAlgBytes + ChkAlgBytes + KeyNumBytes + KdfSaltBytes +
                                         KdfOpsLimitBytes + KdfMemLimitBytes + CipherBytes + ChecksumBytes;

        public static readonly byte[] SigAlg = {(byte) 'E', (byte) 'd'};
        public static readonly byte[] KdfAlg = {(byte) 'S', (byte) 'c'};
        public static readonly byte[] ChkAlg = {(byte) 'B', (byte) '2'};

        public static bool Create(string pathArgument, bool warnIfExists, bool allowMissing, IKeyCapture capture)
        {
            if (!TryResolveKeyPath(pathArgument, out var keyFilePath, warnIfExists, allowMissing))
                return false;
            var keyFileStream =
                new FileStream(keyFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (!TryGenerateKeyFile(keyFileStream, Console.Out, Console.Error, capture))
                return false;
            keyFileStream.Dispose();
            Console.Out.WriteLine(Strings.KeyFileSuccess);
            return true;
        }

        public static unsafe bool TryCapturePassword(string instructions, IPersistedKeyCapture @in, TextWriter @out,
            TextWriter error, out byte* password, out int passwordLength)
        {
            if (@in.TryReadPersisted(out password, out passwordLength))
                return true;
            var result = TryCapturePassword(instructions, @in as IKeyCapture, @out, error, out password,
                out passwordLength);
            if (result)
                @in.Sink(password, passwordLength);
            return result;
        }

        public static unsafe bool TryCapturePassword(string instructions, IKeyCapture @in, TextWriter @out,
            TextWriter error, out byte* password, out int passwordLength)
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

                Console.ForegroundColor = ConsoleColor.Cyan;
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
                            @out.Write(BackspaceSpaceBackspace);
                        }

                        continue;
                    }

                    initPwd.WriteByte((byte) key.KeyChar);
                    key = default;
                    @in.OnKeyRead(@out);
                    initPwdLength++;
                } while (key.Key != ConsoleKey.Enter && initPwdLength < passwordMaxBytes);

                Console.ResetColor();

                var confirmPwdLength = 0;
                using var confirmPwd =
                    new UnmanagedMemoryStream((byte*) passwordConfirm, 0, passwordMaxBytes, FileAccess.Write);

                @out.WriteLine();
                @out.Write(Strings.ConfirmPasswordPrompt);

                Console.ForegroundColor = ConsoleColor.Cyan;
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
                            @out.Write(BackspaceSpaceBackspace);
                        }

                        continue;
                    }

                    confirmPwd.WriteByte((byte) key.KeyChar);
                    key = default;
                    @in.OnKeyRead(@out);
                    confirmPwdLength++;
                } while (key.Key != ConsoleKey.Enter && confirmPwdLength < passwordMaxBytes);

                Console.ResetColor();

                @out.WriteLine();

                passwordLength = Math.Max(initPwdLength, confirmPwdLength);
                if (passwordLength == 0)
                {
                    passwordLength = -1;
                    error.WriteErrorLine(Strings.InvalidPasswordLength);
                    NativeMethods.sodium_free(password);
                    password = default;
                    return false;
                }

                if (initPwdLength != confirmPwdLength)
                {
                    passwordLength = -1;
                    error.WriteErrorLine(Strings.PasswordMismatch);
                    NativeMethods.sodium_free(password);
                    password = default;
                    return false;
                }

                if (NativeMethods.sodium_memcmp(password, passwordConfirm, passwordLength) != 0)
                {
                    error.WriteErrorLine(Strings.PasswordMismatch);
                    NativeMethods.sodium_free(password);
                    password = default;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                error.WriteErrorLine(Strings.PasswordFailure);
                NativeMethods.sodium_free(password);
                password = default;
                return false;
            }
            finally
            {
                NativeMethods.sodium_free(passwordConfirm);
                Console.ResetColor();
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

        public static unsafe bool TryGenerateKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error,
            IKeyCapture keyCapture)
        {
            keyCapture ??= Constants.ConsoleKeyCapture;

            if (!TryCapturePassword(Strings.GenerateKeyInstructions, keyCapture, @out, error, out var password,
                out var passwordLength))
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
                    {
                        for (var i = 0; i < SigAlgBytes; i++)
                            checksumInput[offset++] = src[i];
                    }

                    for (var i = 0; i < KeyNumBytes; i++)
                        checksumInput[offset++] = keyNumber[i];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        checksumInput[offset++] = sk[i];

                    if (NativeMethods.crypto_generichash(checksum, ChecksumBytes, checksumInput, ChecksumInputBytes,
                        null, 0) != 0)
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

                    if (NativeMethods.crypto_pwhash_scryptsalsa208sha256(stream, CipherBytes, password,
                        (ulong) passwordLength, kdfSalt, opsLimit, memLimit) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_pwhash_scryptsalsa208sha256));

                    xor = Xor(cipher, stream, CipherBytes);
                }
                finally
                {
                    NativeMethods.sodium_free(password);
                    NativeMethods.sodium_free(cipher);
                    NativeMethods.sodium_free(stream);
                }

                @out.WriteLine(Strings.EncryptionCompleteMessage);

                // 
                // Write key file: (SigAlg || KdfAlg || ChkAlg || KeyNum || KdfSalt || OpsLimit || MemLimit || Cipher || Checksum)
                var file = (byte*) NativeMethods.sodium_malloc(KeyFileBytes);
                try
                {
                    offset = 0;

                    fixed (byte* src = SigAlg)
                    {
                        for (var i = 0; i < SigAlgBytes; i++)
                            file[offset++] = src[i];
                    }

                    fixed (byte* src = KdfAlg)
                    {
                        for (var i = 0; i < KdfAlgBytes; i++)
                            file[offset++] = src[i];
                    }

                    fixed (byte* src = ChkAlg)
                    {
                        for (var i = 0; i < ChkAlgBytes; i++)
                            file[offset++] = src[i];
                    }

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
                        error.WriteErrorLine(Strings.InvalidKeyFileBuffer);
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
                    using var mmf = MemoryMappedFile.CreateFromFile(keyFileStream, null, KeyFileBytes,
                        MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
                    using var uvs = mmf.CreateViewStream(0, KeyFileBytes, MemoryMappedFileAccess.ReadWrite);
                    for (var i = 0; i < (int) KeyFileBytes; i++)
                        uvs.WriteByte(file[i]);
                    uvs.Flush();
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
                error.WriteErrorLine(Strings.KeyFileGenerateFailure);
                return false;
            }
        }

        public static unsafe bool TryLoadKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error,
            out byte* secretKey, IPersistedKeyCapture capture)
        {
            if (capture == default)
                throw new InvalidOperationException(Strings.InvalidKeyCapture);
            secretKey = default;
            return TryCapturePassword(Strings.LoadKeyInstructions, capture, @out, error, out var password,
                       out var passwordLength) &&
                   TryLoadKeyFile(keyFileStream, @out, error, ref secretKey, password, passwordLength, true);
        }

        public static unsafe bool TryLoadKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error,
            out byte* secretKey, IKeyCapture capture)
        {
            if (capture == default)
                throw new InvalidOperationException(Strings.InvalidKeyCapture);
            secretKey = default;
            return TryCapturePassword(Strings.LoadKeyInstructions, capture, @out, error, out var password,
                       out var passwordLength) &&
                   TryLoadKeyFile(keyFileStream, @out, error, ref secretKey, password, passwordLength, false);
        }

        private static unsafe bool TryLoadKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error,
            ref byte* secretKey, byte* password, int passwordLength, bool leaveOpen)
        {
            // 
            // Read key file: (SigAlg || KdfAlg || ChkAlg || KeyNum || KdfSalt || OpsLimit || MemLimit || Cipher || Checksum)
            try
            {
                using var mmf = MemoryMappedFile.CreateFromFile(keyFileStream, null, KeyFileBytes,
                    MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                using var uvs = mmf.CreateViewStream(0, KeyFileBytes, MemoryMappedFileAccess.Read);

                var sigAlg = NativeMethods.sodium_malloc(SigAlgBytes);
                try
                {
                    var buffer = (byte*) sigAlg;
                    for (var i = 0; i < SigAlgBytes; i++)
                        buffer[i] = (byte) uvs.ReadByte();

                    fixed (void* src = SigAlg)
                    {
                        if (NativeMethods.sodium_memcmp(sigAlg, src, SigAlg.Length) != 0)
                        {
                            error.WriteErrorLine(Strings.InvalidSignatureAlgorithm);
                            return false;
                        }
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
                    {
                        if (NativeMethods.sodium_memcmp(kdfAlg, src, KdfAlgBytes) != 0)
                        {
                            error.WriteErrorLine(Strings.InvalidKeyDerivationFunction);
                            return false;
                        }
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
                    {
                        if (NativeMethods.sodium_memcmp(chkAlg, src, ChkAlgBytes) != 0)
                        {
                            error.WriteErrorLine(Strings.InvalidChecksumFunction);
                            return false;
                        }
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
                    error.WriteErrorLine(Strings.InvalidKeyFileBuffer);
                    return false;
                }

                @out.Write(Strings.DecryptionInProgressMessage);

                var stream = (byte*) NativeMethods.sodium_malloc(CipherBytes);
                byte* xor;
                try
                {
                    if (NativeMethods.crypto_pwhash_scryptsalsa208sha256(stream, CipherBytes, password,
                        (ulong) passwordLength,
                        kdfSalt, opsLimit, memLimit) != 0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_pwhash_scryptsalsa208sha256));

                    xor = Xor(fileCipher, stream, CipherBytes);
                }
                finally
                {
                    if (!leaveOpen)
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
                        error.WriteErrorLine(Strings.InvalidDecryptionPassword);
                        return false;
                    }

                    if (NativeMethods.sodium_memcmp(fileCipherKeyNumber, fileKeyNumber, KeyNumBytes) != 0)
                    {
                        NativeMethods.sodium_free(sk);
                        error.WriteErrorLine(Strings.InvalidKeyFileKeyNumber);
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
                    {
                        for (var i = 0; i < SigAlgBytes; i++)
                            checksumInput[offset++] = src[i];
                    }

                    for (var i = 0; i < KeyNumBytes; i++)
                        checksumInput[offset++] = fileCipherKeyNumber[i];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        checksumInput[offset++] = sk[i];

                    if (NativeMethods.crypto_generichash(checksum, ChecksumBytes, checksumInput, ChecksumInputBytes,
                            null, 0) !=
                        0)
                        throw new InvalidOperationException(nameof(NativeMethods.crypto_generichash));

                    if (NativeMethods.sodium_memcmp(checksum, fileCipherChecksum, ChecksumBytes) != 0)
                    {
                        NativeMethods.sodium_free(sk);
                        error.WriteErrorLine(Strings.InvalidKeyFileChecksum);
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
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                error.WriteErrorLine(Strings.KeyFileLoadFailure);
                return false;
            }
        }

        public static bool TryResolveKeyPath(string pathArgument, out string fullKeyPath, bool warnIfExists,
            bool allowMissing)
        {
            fullKeyPath = default;

            if (pathArgument == Constants.DefaultKeyFilePath)
                Directory.CreateDirectory(".egregore");

            if (pathArgument != Constants.DefaultKeyFilePath &&
                Path.GetFileName(pathArgument).IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                Console.Error.WriteErrorLine(Strings.InvalidCharactersInPath);
                return false;
            }

            try
            {
                fullKeyPath = Path.GetFullPath(pathArgument);
                if (!Path.HasExtension(fullKeyPath))
                    fullKeyPath = Path.ChangeExtension(pathArgument, ".key");
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.Error.WriteErrorLine(Strings.InvalidKeyFilePath);
                return false;
            }

            if (warnIfExists && File.Exists(fullKeyPath))
            {
                Console.Error.WriteWarningLine(Strings.KeyFileAlreadyExists);
            }
            else if (!allowMissing && !File.Exists(fullKeyPath))
            {
                Console.Error.WriteErrorLine(Strings.KeyFileIsMissing);
                return false;
            }

            return true;
        }
    }
}