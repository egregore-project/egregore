// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace egregore.Data.Tests.Fixtures
{
    public sealed class RecordStoreFixture : IDisposable
    {
        public RecordStoreFixture()
        {
            var store = new LightningRecordStore(Guid.NewGuid().ToString(), new NoSearchIndex(), new RecordEvents(),
                new LogObjectTypeProvider());
            store.Init($"{Guid.NewGuid()}.egg");
            Store = store;
        }

        public IRecordStore Store { get; }

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