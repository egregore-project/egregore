// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace egregore.Tests
{
    public sealed class LogStoreFixture : IDisposable
    {
        public ILogStore Store { get; }

        public LogStoreFixture()
        {
            var store = new LogStore($"{Guid.NewGuid()}.egg");
            store.Init();
            Store = store;
        }

        public void Dispose()
        {
            var dataFile = Store?.DataFile;
            if (string.IsNullOrWhiteSpace(dataFile))
                return;

            try
            {
                File.Delete(dataFile);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}