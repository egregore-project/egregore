// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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
        public void Succeeds_password_capture_when_password_is_confirmed_correctly(string plaintext, string plaintextConfirm = null)
        {
            var capture = new TestKeyCapture(plaintext, plaintextConfirm ?? plaintext);
            unsafe
            {
                var result = PasswordStorage.TryCapturePassword("test", capture, Console.Out, Console.Error, out var password, out var passwordLength);
                Assert.True(result);
                Assert.Equal(plaintext.Length, passwordLength);
                NativeMethods.sodium_free(password);
            }
        }

        [Theory]
        [InlineData("rosebud")]
        public void Fails_password_capture_with_empty_password(string plaintextConfirm)
        {
            var capture = new TestKeyCapture(string.Empty, plaintextConfirm);
            unsafe
            {
                var result = PasswordStorage.TryCapturePassword("test", capture, Console.Out, Console.Error, out var password, out var passwordLength);
                Assert.False(result);
                Assert.NotEqual(plaintextConfirm.Length, passwordLength);
                NativeMethods.sodium_free(password);
            }
        }

        [Theory]
        [InlineData("rosebud")]
        public void Fails_password_capture_with_empty_confirm_password(string plaintext)
        {
            var capture = new TestKeyCapture(plaintext, string.Empty);
            unsafe
            {
                var result = PasswordStorage.TryCapturePassword("test", capture, Console.Out, Console.Error, out var password, out var passwordLength);
                Assert.False(result);
                Assert.NotEqual(plaintext.Length, passwordLength);
                NativeMethods.sodium_free(password);
            }
        }

        [Theory]
        [InlineData("rosebud")]
        [InlineData("rosebud", "rosebud\bt")]
        public void Fails_password_capture_when_confirm_password_is_incorrect(string plaintext, string plaintextConfirm = null)
        {
            var capture = new TestKeyCapture(plaintext, plaintextConfirm ?? $"{plaintext}wrong");
            unsafe
            {
                var result = PasswordStorage.TryCapturePassword("test", capture, Console.Out, Console.Error, out var password, out var passwordLength);
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
                var keyPath = Path.GetTempFileName();

                var @out = new XunitDuplexTextWriter(_output, Console.Out);
                var error = new XunitDuplexTextWriter(_output, Console.Error);
                
                var generatedKeyFile = PasswordStorage.TryGenerateKeyFile(keyPath, @out, error, new TestKeyCapture(plaintext, plaintext));
                Assert.True(generatedKeyFile);

                var loadedKeyFile = PasswordStorage.TryLoadKeyFile(keyPath, @out, error, out var secretKey, new TestKeyCapture(plaintext, plaintext));
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
                var keyPath = Path.GetTempFileName();

                var @out = new XunitDuplexTextWriter(_output, Console.Out);
                var error = new XunitDuplexTextWriter(_output, Console.Error);
                
                var generatedKeyFile = PasswordStorage.TryGenerateKeyFile(keyPath, @out, error, new TestKeyCapture(plaintext, plaintext));
                Assert.True(generatedKeyFile, nameof(generatedKeyFile));

                var loadedKeyFile = PasswordStorage.TryLoadKeyFile(keyPath, @out, error, out var secretKey, new TestKeyCapture($"{plaintext}wrong", $"{plaintext}wrong"));
                Assert.False(loadedKeyFile, nameof(loadedKeyFile));
                Assert.True(secretKey == default(byte*), nameof(secretKey));
            }
        }
    }
}