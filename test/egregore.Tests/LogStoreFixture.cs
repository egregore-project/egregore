// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace egregore.Tests
{
    public sealed class LogStoreFixture : IDisposable
    {
        public ILogStore Store { get; }

        public LogStoreFixture()
        {
            var store = new LightningLogStore($"{Guid.NewGuid()}.egg");
            store.Init();
            Store = store;
        }

        public void Dispose()
        {
            var dataFile = Store?.DataFile;
            if (string.IsNullOrWhiteSpace(dataFile))
                return;
            Store?.Purge();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}