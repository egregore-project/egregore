// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using egregore.Data;

namespace egregore
{
    public interface IRecordStore
    {
        string DataFile { get; }
        Task<ulong> GetLengthAsync();

        Task<ulong> AddRecordAsync(Record record, byte[] secretKey = null);
        Task<Record> GetByIdAsync(Guid uuid);

        void Destroy(bool destroySequence);
    }
}