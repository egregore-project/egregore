// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace egregore.Ontology
{
    public interface ILogStore
    {
        string DataFile { get; }
        Task<ulong> GetLengthAsync();
        Task<ulong> AddEntryAsync(LogEntry entry, byte[] secretKey = null);
        IEnumerable<LogEntry> StreamEntries(ulong startingFrom = 0UL, byte[] secretKey = null);
        
        void Init(string path);
        void Destroy();
    }
}