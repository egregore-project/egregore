// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Data;
using egregore.Ontology;

namespace egregore.Tests.Helpers
{
    public sealed class LogStoreFixture : IDisposable
    {
        public LogStoreFixture()
        {
            var store = new LightningLogStore(new LogObjectTypeProvider());
            store.Init($"{Guid.NewGuid()}");
            Store = store;
        }

        public ILogStore Store { get; }

        public void Dispose()
        {
            var dataFile = Store?.DataFile;
            if (string.IsNullOrWhiteSpace(dataFile))
                return;
            Store?.Destroy();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}