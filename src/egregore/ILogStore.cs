// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace egregore
{
    public interface ILogStore
    {
        string DataFile { get; }
        Task<ulong> GetLengthAsync();
        Task<ulong> AddEntryAsync(LogEntry entry, byte[] secretKey = null);
        IEnumerable<LogEntry> StreamEntries(ulong startingFrom = 0UL, byte[] secretKey = null);
        void Purge();
    }
}