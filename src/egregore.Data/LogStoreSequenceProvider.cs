// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;

namespace egregore.Data
{
    internal sealed class LogStoreSequenceProvider : ISequenceProvider
    {
        private readonly ILogStore _store;

        public LogStoreSequenceProvider(ILogStore store)
        {
            _store = store;
        }

        public async Task<ulong> GetNextValueAsync()
        {
            return await _store.GetLengthAsync();
        }

        public void Destroy()
        {
            _store.Destroy();
        }

        public void Dispose()
        {
        }
    }
}