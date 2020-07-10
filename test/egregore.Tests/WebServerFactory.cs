// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using egregore.IO;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace egregore.Tests
{
    public sealed class WebServerFactory : WebApplicationFactory<WebServer>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            var keyFilePath = Path.GetTempFileName();
            var eggPath = Path.GetTempFileName();

            File.WriteAllBytes(keyFilePath, new byte[KeyFileManager.KeyFileBytes]);
            
            var password =  $"{Guid.NewGuid()}";
            IKeyCapture capture = new PlaintextKeyCapture(password, password);
            
            Assert.True(KeyFileManager.Create(keyFilePath, false, true, capture));

            Crypto.Initialize();
            Program.keyFilePath = keyFilePath;
            Program.keyFileStream = new FileStream(Program.keyFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            
            return WebServer.CreateHostBuilder(eggPath, capture);
        }
    }
}