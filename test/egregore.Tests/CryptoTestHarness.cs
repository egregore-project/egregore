// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.IO;
using Xunit;
using Xunit.Abstractions;

namespace egregore.Tests
{
    public static class CryptoTestHarness
    {
        private static readonly object Sync = new object();

        internal static byte[] GenerateKeyFile(ITestOutputHelper output, IKeyCapture capture, IKeyFileService service)
        {
            lock (Sync)
            {
                var @out = new XunitDuplexTextWriter(output, Console.Out);
                var error = new XunitDuplexTextWriter(output, Console.Error);
                Assert.True(KeyFileManager.TryGenerateKeyFile(service.GetKeyFileStream(), @out, error, capture));
                capture.Reset();
                return Crypto.SigningPublicKeyFromSigningKey(service, capture);
            }
        }
    }
}