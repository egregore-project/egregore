// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using egregore.Data;

namespace egregore.Tests.Data
{
    public sealed class RecordStoreFixture : IDisposable
    {
        public IRecordStore Store { get; }

        public RecordStoreFixture()
        {
            var store = new LightningRecordStore($"{Guid.NewGuid()}.egg", $"{Guid.NewGuid()}");
            store.Init();
            Store = store;
        }

        public void Dispose()
        {
            var dataFile = Store?.DataFile;
            if (string.IsNullOrWhiteSpace(dataFile))
                return;
            Store?.Destroy(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}