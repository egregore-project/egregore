// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace egregore.Data
{
    public interface ILogStore : IDataStore
    {
        Task<ulong> GetLengthAsync(CancellationToken cancellationToken = default);

        Task<ulong> AddEntryAsync(LogEntry entry, byte[] secretKey = null,
            CancellationToken cancellationToken = default);

        IEnumerable<LogEntry> StreamEntries(ulong startingFrom = 0UL, byte[] secretKey = null,
            CancellationToken cancellationToken = default);

        void Destroy();
    }
}