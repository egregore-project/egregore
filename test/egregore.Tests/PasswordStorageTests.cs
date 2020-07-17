// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using egregore.IO;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public class PasswordStorageTests
    {
        private readonly ITestOutputHelper _output;

        public PasswordStorageTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("rosebud", "rot\bsebud")]
        [InlineData("@purple monkey dishwasher!")]
        public void Succeeds_password_capture_when_password_is_confirmed_correctly(string plaintext,
            string plaintextConfirm = null)
        {
            var capture = new PlaintextKeyCapture(plaintext, plaintextConfirm ?? plaintext);
            unsafe
            {
                var result = KeyFileManager.TryCapturePassword("test", capture, Console.Out, Console.Error,
                    out var password, out var passwordLength);
                Assert.True(result);
                Assert.Equal(plaintext.Length, passwordLength);
                NativeMethods.sodium_free(password);
            }
        }

        [Theory]
        [InlineData("rosebud")]
        public void Fails_password_capture_with_empty_password(string plaintextConfirm)
        {
            var capture = new PlaintextKeyCapture(string.Empty, plaintextConfirm);
            unsafe
            {
                var result = KeyFileManager.TryCapturePassword("test", capture, Console.Out, Console.Error,
                    out var password, out var passwordLength);
                Assert.False(result);
                Assert.NotEqual(plaintextConfirm.Length, passwordLength);
                NativeMethods.sodium_free(password);
            }
        }

        [Theory]
        [InlineData("rosebud")]
        public void Fails_password_capture_with_empty_confirm_password(string plaintext)
        {
            var capture = new PlaintextKeyCapture(plaintext, string.Empty);
            unsafe
            {
                var result = KeyFileManager.TryCapturePassword("test", capture, Console.Out, Console.Error,
                    out var password, out var passwordLength);
                Assert.False(result);
                Assert.NotEqual(plaintext.Length, passwordLength);
                NativeMethods.sodium_free(password);
            }
        }

        [Theory]
        [InlineData("rosebud")]
        [InlineData("rosebud", "rosebud\bt")]
        public void Fails_password_capture_when_confirm_password_is_incorrect(string plaintext,
            string plaintextConfirm = null)
        {
            var capture = new PlaintextKeyCapture(plaintext, plaintextConfirm ?? $"{plaintext}wrong");
            unsafe
            {
                var result = KeyFileManager.TryCapturePassword("test", capture, Console.Out, Console.Error,
                    out var password, out var passwordLength);
                Assert.False(result);
                Assert.True(password == default(byte*));
            }
        }

        [Theory]
        [InlineData("rosebud")]
        public void Can_save_and_load_key_file_with_correct_password(string plaintext)
        {
            unsafe
            {
                var keyFilePath = Path.GetTempFileName();
                var keyFileStream = File.Open(keyFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                var @out = new XunitDuplexTextWriter(_output, Console.Out);
                var error = new XunitDuplexTextWriter(_output, Console.Error);

                var generatedKeyFile = KeyFileManager.TryGenerateKeyFile(keyFileStream, @out, error,
                    new PlaintextKeyCapture(plaintext, plaintext));
                Assert.True(generatedKeyFile);

                keyFileStream.Dispose();
                keyFileStream = File.OpenRead(keyFilePath);

                var loadedKeyFile = KeyFileManager.TryLoadKeyFile(keyFileStream, @out, error, out var secretKey,
                    new PlaintextKeyCapture(plaintext, plaintext));
                Assert.True(loadedKeyFile);
                NativeMethods.sodium_free(secretKey);
            }
        }

        [Theory]
        [InlineData("rosebud")]
        public void Cannot_load_saved_key_file_with_incorrect_password(string plaintext)
        {
            unsafe
            {
                var keyFilePath = Path.GetTempFileName();
                var keyFileStream = File.Open(keyFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                var @out = new XunitDuplexTextWriter(_output, Console.Out);
                var error = new XunitDuplexTextWriter(_output, Console.Error);

                var generatedKeyFile = KeyFileManager.TryGenerateKeyFile(keyFileStream, @out, error,
                    new PlaintextKeyCapture(plaintext, plaintext));
                Assert.True(generatedKeyFile, nameof(generatedKeyFile));

                keyFileStream.Dispose();
                keyFileStream = File.OpenRead(keyFilePath);

                var loadedKeyFile = KeyFileManager.TryLoadKeyFile(keyFileStream, @out, error, out var secretKey,
                    new PlaintextKeyCapture($"{plaintext}wrong", $"{plaintext}wrong"));
                Assert.False(loadedKeyFile, nameof(loadedKeyFile));
                Assert.True(secretKey == default(byte*), nameof(secretKey));
            }
        }
    }
}