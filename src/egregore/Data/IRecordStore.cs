// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace egregore.Data
{
    public interface IRecordStore : IDataStore
    {
        Task<ulong> AddRecordAsync(Record record, byte[] secretKey = null, CancellationToken cancellationToken = default);
        
        Task<ulong> GetLengthAsync(CancellationToken cancellationToken = default);
        Task<ulong> GetLengthByTypeAsync(string type, CancellationToken cancellationToken = default);
        Task<Record> GetByIdAsync(Guid uuid, CancellationToken cancellationToken = default);
        Task<IEnumerable<Record>> GetByTypeAsync(string type, out ulong total, CancellationToken cancellationToken = default);
        Task<IEnumerable<Record>> GetByColumnValueAsync(string type, string name, string value, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Record> StreamRecordsAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<Record> SearchAsync(string query, CancellationToken cancellationToken = default);

        void Destroy(bool destroySequence);
    }
}