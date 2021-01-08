// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using egregore.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace egregore.Tests.Helpers
{
    public sealed class WebServerFactory : WebApplicationFactory<Startup>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            var keyFilePath = Path.GetTempFileName();
            var eggPath = Path.GetTempFileName();

            File.WriteAllBytes(keyFilePath, new byte[KeyFileManager.KeyFileBytes]);

            var password = $"{Guid.NewGuid()}";
            IKeyCapture capture = new PlaintextKeyCapture(password, password);

            Assert.True(KeyFileManager.Create(keyFilePath, false, true, capture));

            Crypto.Initialize();
            Program.keyFilePath = keyFilePath;
            Program.keyFileStream = new FileStream(Program.keyFilePath, FileMode.Open, FileAccess.Read, FileShare.None);

            return Program.CreateHostBuilder(null, eggPath, capture);
        }
    }
}