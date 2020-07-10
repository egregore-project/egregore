// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using LightningDB;

namespace egregore.Data
{
    internal sealed class LightningRecordStore : LightningDataStore, IRecordStore
    {
        private readonly ILogObjectTypeProvider _typeProvider;
        private readonly ILogEntryHashProvider _hashProvider;
        private readonly RecordKeyBuilder _keyBuilder;
        private readonly ISequenceProvider _sequence;

        public LightningRecordStore(string path, string sequence = Constants.DefaultSequence) : base(path)
        {
            _typeProvider = new LogObjectTypeProvider();
            _hashProvider = new LogEntryHashProvider(_typeProvider);
            _keyBuilder = new RecordKeyBuilder();
            _sequence = new GlobalSequenceProvider(sequence);
        }

        public async Task<ulong> AddRecordAsync(Record record, byte[] secretKey = null)
        {
            if (record.Uuid == default)
                record.Uuid = Guid.NewGuid();

            await using var ms = new MemoryStream();
            await using var bw = new BinaryWriter(ms);
            var context = new LogSerializeContext(bw, _typeProvider);
            record.Serialize(context, false);

            var sequence = await _sequence.GetNextValueAsync();
            var key = _keyBuilder.BuildKey(record);
            var value = ms.ToArray();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase();
            tx.Put(db, key, value, PutOptions.NoOverwrite);
            tx.Commit();

            record.Index = sequence;
            return sequence + 1;
        }

        public void Destroy(bool destroySequence)
        {
            Destroy();
            if (destroySequence)
                _sequence.Destroy();
        }

        protected override void Dispose(bool disposing)
        {
            _sequence?.Dispose();
        }
    }
}