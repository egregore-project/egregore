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

using static egregore.Cryptography.NativeMethods;

namespace egregore.Cryptography
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
    public static class KeyFileManager
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

        public static unsafe bool TryCapturePassword(string instructions, IPersistedKeyCapture @in, TextWriter @out, TextWriter error, out byte* password, out int passwordLength)
        {
            if (@in.TryReadPersisted(out password, out passwordLength))
                return true;
            var result = TryCapturePassword(instructions, @in as IKeyCapture, @out, error, out password, out passwordLength);
            if (result)
                @in.Sink(password, passwordLength);
            return result;
        }

        public static unsafe bool TryCapturePassword(string instructions, IKeyCapture @in, TextWriter @out, TextWriter error, out byte* password, out int passwordLength)
        {
            const int passwordMaxBytes = 1024;
            password = (byte*) sodium_malloc(passwordMaxBytes);
            var passwordConfirm = sodium_malloc(passwordMaxBytes);
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
                    sodium_free(password);
                    password = default;
                    return false;
                }

                if (initPwdLength != confirmPwdLength)
                {
                    passwordLength = -1;
                    error.WriteErrorLine(Strings.PasswordMismatch);
                    sodium_free(password);
                    password = default;
                    return false;
                }

                if (sodium_memcmp(password, passwordConfirm, passwordLength) != 0)
                {
                    error.WriteErrorLine(Strings.PasswordMismatch);
                    sodium_free(password);
                    password = default;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                error.WriteErrorLine(Strings.PasswordFailure);
                sodium_free(password);
                password = default;
                return false;
            }
            finally
            {
                sodium_free(passwordConfirm);
                Console.ResetColor();
            }

            return true;
        }

        public static unsafe byte* Xor(byte* dst, byte* src, uint len)
        {
            var xor = (byte*) sodium_malloc(len);
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

            var sk = (byte*) sodium_malloc(Crypto.SecretKeyBytes);
            try
            {
                var pk = (byte*) sodium_malloc(Crypto.PublicKeyBytes);

                try
                {
                    // Create a new signing key pair:
                    if (crypto_sign_keypair(pk, sk) != 0)
                        throw new InvalidOperationException(nameof(crypto_sign_keypair));
                }
                finally
                {
                    sodium_free(pk);
                }

                // Create key number to mix with the checksum:
                var keyNumber = (byte*) sodium_malloc(KeyNumBytes);

                // Checksum = Blake2B(SigAlg || KeyNumber || SecretKey):
                var offset = 0;
                var checksumInput = (byte*) sodium_malloc(ChecksumInputBytes);
                var checksum = (byte*) sodium_malloc(ChecksumBytes);
                try
                {
                    randombytes_buf(keyNumber, KeyNumBytes);

                    fixed (byte* src = SigAlg)
                    {
                        for (var i = 0; i < SigAlgBytes; i++)
                            checksumInput[offset++] = src[i];
                    }

                    for (var i = 0; i < KeyNumBytes; i++)
                        checksumInput[offset++] = keyNumber[i];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        checksumInput[offset++] = sk[i];

                    if (crypto_generichash(checksum, ChecksumBytes, checksumInput, ChecksumInputBytes,
                        null, 0) != 0)
                        throw new InvalidOperationException(nameof(crypto_generichash));
                }
                finally
                {
                    sodium_free(checksumInput);
                }

                //
                // Prepare cipher block for encryption: (KeyNum || SecretKey || Checksum)
                var cipher = (byte*) sodium_malloc(CipherBytes);
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
                    sodium_free(sk);
                }

                @out.Write(Strings.EncryptionInProgressMessage);

                //
                // Encrypt the secret key by mixing it with another key derived from the password:
                // SecretKey ^ crypto_pwhash_scryptsalsa208sha256(password || salt || opsLimit || memLimit)):
                const ulong opsLimit = KdfOpsLimit;
                const int memLimit = KdfMemLimit;
                var kdfSalt = (byte*) sodium_malloc(KdfSaltBytes);
                var stream = (byte*) sodium_malloc(CipherBytes);
                byte* xor;
                try
                {
                    randombytes_buf(kdfSalt, KdfSaltBytes);

                    if (crypto_pwhash_scryptsalsa208sha256(stream, CipherBytes, password,
                        (ulong) passwordLength, kdfSalt, opsLimit, memLimit) != 0)
                        throw new InvalidOperationException(nameof(crypto_pwhash_scryptsalsa208sha256));

                    xor = Xor(cipher, stream, CipherBytes);
                }
                finally
                {
                    sodium_free(password);
                    sodium_free(cipher);
                    sodium_free(stream);
                }

                @out.WriteLine(Strings.EncryptionCompleteMessage);

                // 
                // Write key file: (SigAlg || KdfAlg || ChkAlg || KeyNum || KdfSalt || OpsLimit || MemLimit || Cipher || Checksum)
                var file = (byte*) sodium_malloc(KeyFileBytes);
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
                        sodium_free(file);
                        return false;
                    }
                }
                finally
                {
                    sodium_free(keyNumber);
                    sodium_free(kdfSalt);
                    sodium_free(checksum);
                    sodium_free(xor);
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
                    sodium_free(file);
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

        public static unsafe bool TryLoadKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error, out byte* secretKey, IPersistedKeyCapture capture)
        {
            if (capture == default)
                throw new InvalidOperationException(Strings.InvalidKeyCapture);
            secretKey = default;
            return TryCapturePassword(Strings.LoadKeyInstructions, capture, @out, error, out var password, out var passwordLength) &&
                   TryLoadKeyFile(keyFileStream, @out, error, ref secretKey, password, passwordLength, true);
        }

        public static unsafe bool TryLoadKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error, out byte* secretKey, IKeyCapture capture)
        {
            if (capture == default)
                throw new InvalidOperationException(Strings.InvalidKeyCapture);
            secretKey = default;
            return TryCapturePassword(Strings.LoadKeyInstructions, capture, @out, error, out var password, out var passwordLength) && 
                   TryLoadKeyFile(keyFileStream, @out, error, ref secretKey, password, passwordLength, false);
        }

        private static unsafe bool TryLoadKeyFile(FileStream keyFileStream, TextWriter @out, TextWriter error, ref byte* secretKey, byte* password, int passwordLength, bool leaveOpen)
        {
            // 
            // Read key file: (SigAlg || KdfAlg || ChkAlg || KeyNum || KdfSalt || OpsLimit || MemLimit || Cipher || Checksum)
            try
            {
                using var mmf = MemoryMappedFile.CreateFromFile(keyFileStream, null, KeyFileBytes, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                using var uvs = mmf.CreateViewStream(0, KeyFileBytes, MemoryMappedFileAccess.Read);

                var sigAlg = sodium_malloc(SigAlgBytes);
                try
                {
                    var buffer = (byte*) sigAlg;
                    for (var i = 0; i < SigAlgBytes; i++)
                        buffer[i] = (byte) uvs.ReadByte();

                    fixed (void* src = SigAlg)
                    {
                        if (sodium_memcmp(sigAlg, src, SigAlg.Length) != 0)
                        {
                            error.WriteErrorLine(Strings.InvalidSignatureAlgorithm);
                            return false;
                        }
                    }
                }
                finally
                {
                    sodium_free(sigAlg); // since we only have one, we can toss this
                }

                var kdfAlg = sodium_malloc(KdfAlgBytes);
                try
                {
                    var buffer = (byte*) kdfAlg;
                    for (var i = 0; i < KdfAlgBytes; i++)
                        buffer[i] = (byte) uvs.ReadByte();

                    fixed (void* src = KdfAlg)
                    {
                        if (sodium_memcmp(kdfAlg, src, KdfAlgBytes) != 0)
                        {
                            error.WriteErrorLine(Strings.InvalidKeyDerivationFunction);
                            return false;
                        }
                    }
                }
                finally
                {
                    sodium_free(kdfAlg); // since we only have one, we can toss this
                }

                var chkAlg = sodium_malloc(ChkAlgBytes);
                try
                {
                    var buffer = (byte*) chkAlg;
                    for (var i = 0; i < ChkAlgBytes; i++)
                        buffer[i] = (byte) uvs.ReadByte();

                    fixed (void* src = ChkAlg)
                    {
                        if (sodium_memcmp(chkAlg, src, ChkAlgBytes) != 0)
                        {
                            error.WriteErrorLine(Strings.InvalidChecksumFunction);
                            return false;
                        }
                    }
                }
                finally
                {
                    sodium_free(chkAlg); // since we only have one, we can toss this
                }

                var fileKeyNumber = (byte*) sodium_malloc(KeyNumBytes);
                for (var i = 0; i < KeyNumBytes; i++)
                    fileKeyNumber[i] = (byte) uvs.ReadByte();

                var kdfSalt = (byte*) sodium_malloc(KdfSaltBytes);
                for (var i = 0; i < KdfSaltBytes; i++)
                    kdfSalt[i] = (byte) uvs.ReadByte();

                var opsLimitData = (byte*) sodium_malloc(KdfOpsLimitBytes);
                for (var i = 0; i < KdfOpsLimitBytes; i++)
                    opsLimitData[i] = (byte) uvs.ReadByte();
                var opsLimit = BitConverter.ToUInt64(new ReadOnlySpan<byte>(opsLimitData, KdfOpsLimitBytes));
                sodium_free(opsLimitData);

                var memLimitData = (byte*) sodium_malloc(KdfMemLimitBytes);
                for (var i = 0; i < KdfMemLimitBytes; i++)
                    memLimitData[i] = (byte) uvs.ReadByte();
                var memLimit = BitConverter.ToInt32(new ReadOnlySpan<byte>(memLimitData, KdfMemLimitBytes));
                sodium_free(memLimitData);

                var fileCipher = (byte*) sodium_malloc(CipherBytes);
                for (var i = 0; i < CipherBytes; i++)
                    fileCipher[i] = (byte) uvs.ReadByte();

                var fileChecksum = (byte*) sodium_malloc(ChecksumBytes);
                for (var i = 0; i < ChecksumBytes; i++)
                    fileChecksum[i] = (byte) uvs.ReadByte();

                var eof = uvs.ReadByte();
                if (eof != -1)
                {
                    error.WriteErrorLine(Strings.InvalidKeyFileBuffer);
                    return false;
                }

                @out.Write(Strings.DecryptionInProgressMessage);

                var stream = (byte*) sodium_malloc(CipherBytes);
                byte* xor;
                try
                {
                    if (crypto_pwhash_scryptsalsa208sha256(stream, CipherBytes, password, (ulong) passwordLength, kdfSalt, opsLimit, memLimit) != 0)
                        throw new InvalidOperationException(nameof(crypto_pwhash_scryptsalsa208sha256));

                    xor = Xor(fileCipher, stream, CipherBytes);
                }
                finally
                {
                    if (!leaveOpen)
                        sodium_free(password);
                    sodium_free(kdfSalt);
                    sodium_free(fileCipher);
                    sodium_free(stream);
                }

                @out.WriteLine(Strings.DecryptionCompleteMessage);

                //
                // Deconstruct cipher block for checksum: (KeyNum || SecretKey || Checksum)
                var fileCipherKeyNumber = (byte*) sodium_malloc(KeyNumBytes);
                var sk = (byte*) sodium_malloc(Crypto.SecretKeyBytes);
                var fileCipherChecksum = (byte*) sodium_malloc(ChecksumBytes);
                var offset = 0;
                try
                {
                    for (var i = 0; i < KeyNumBytes; i++)
                        fileCipherKeyNumber[i] = xor[offset++];
                    for (var i = 0; i < Crypto.SecretKeyBytes; i++)
                        sk[i] = xor[offset++];
                    for (var i = 0; i < ChecksumBytes; i++)
                        fileCipherChecksum[i] = xor[offset++];

                    if (sodium_memcmp(fileCipherChecksum, fileChecksum, ChecksumBytes) != 0)
                    {
                        sodium_free(sk);
                        error.WriteErrorLine(Strings.InvalidDecryptionPassword);
                        return false;
                    }

                    if (sodium_memcmp(fileCipherKeyNumber, fileKeyNumber, KeyNumBytes) != 0)
                    {
                        sodium_free(sk);
                        error.WriteErrorLine(Strings.InvalidKeyFileKeyNumber);
                        return false;
                    }
                }
                finally
                {
                    sodium_free(fileKeyNumber);
                    sodium_free(fileChecksum);
                    sodium_free(xor);
                }

                //
                // Checksum = Blake2B(SigAlg || KeyNumber || SecretKey):
                offset = 0;
                var checksumInput = (byte*) sodium_malloc(ChecksumInputBytes);
                var checksum = (byte*) sodium_malloc(ChecksumBytes);
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

                    if (crypto_generichash(checksum, ChecksumBytes, checksumInput, ChecksumInputBytes, null, 0) != 0)
                        throw new InvalidOperationException(nameof(crypto_generichash));

                    if (sodium_memcmp(checksum, fileCipherChecksum, ChecksumBytes) != 0)
                    {
                        sodium_free(sk);
                        error.WriteErrorLine(Strings.InvalidKeyFileChecksum);
                        return false;
                    }
                }
                finally
                {
                    sodium_free(fileCipherKeyNumber);
                    sodium_free(fileCipherChecksum);
                    sodium_free(checksum);
                    sodium_free(checksumInput);
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