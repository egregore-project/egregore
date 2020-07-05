// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Ontology;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public static class CryptoTestHarness
    {
        internal static byte[] GenerateKeyFile(ITestOutputHelper output, IKeyCapture capture, IKeyFileService service)
        {
            var @out = new XunitDuplexTextWriter(output, Console.Out);
            var error = new XunitDuplexTextWriter(output, Console.Error);

            Assert.True(PasswordStorage.TryGenerateKeyFile(service.GetKeyFileStream(), @out, error, capture));

            capture.Reset();
            return Crypto.PublicKeyFromSecretKey(service, capture);
        }
    }
}