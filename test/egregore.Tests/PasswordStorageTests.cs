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

        [Fact]
        public void Succeeds_password_capture_when_password_is_confirmed_correctly()
        {
            var capture = new TestKeyCapture("rosebud", 2);
            unsafe
            {
                var result = PasswordStorage.TryCapturePassword("test", capture, Console.Out, Console.Error, out var password, out _);
                Assert.True(result);    
                NativeMethods.sodium_free(password);
            }
        }

        [Fact]
        public void Fails_password_capture_when_confirm_password_is_incorrect()
        {
            var capture = new TestKeyCapture("rosebud", "rosary");
            unsafe
            {
                var result = PasswordStorage.TryCapturePassword("test", capture, Console.Out, Console.Error, out var password, out _);
                Assert.False(result);    
                Assert.True(password == default(byte*));
            }
        }

        [Fact]
        public void Can_save_and_load_key_file()
        {
            unsafe
            {
                var keyPath = Path.GetTempFileName();

                var @out = new XunitDuplexTextWriter(_output, Console.Out);
                var error = new XunitDuplexTextWriter(_output, Console.Error);
                
                var generatedKeyFile = PasswordStorage.TryGenerateKeyFile(keyPath, @out, error, new TestKeyCapture("rosebud", 2));
                Assert.True(generatedKeyFile);

                var loadedKeyFile = PasswordStorage.TryLoadKeyFile(keyPath, @out, error, out var secretKey, new TestKeyCapture("rosebud", 2));
                Assert.True(loadedKeyFile);
                NativeMethods.sodium_free(secretKey);
            }
        }
    }
}