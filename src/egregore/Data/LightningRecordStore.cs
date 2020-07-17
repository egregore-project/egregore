// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LightningDB;

namespace egregore.Data
{
    internal sealed class LightningRecordStore : LightningDataStore, IRecordStore
    {
        private readonly RecordColumnKeyBuilder _columnKeyBuilder;

        private readonly RecordKeyBuilder _recordKeyBuilder;
        private readonly ISequenceProvider _sequence;
        private readonly ILogObjectTypeProvider _typeProvider;

        public LightningRecordStore(string path, string sequence = Constants.DefaultSequence) : base(path)
        {
            _typeProvider = new LogObjectTypeProvider();
            _recordKeyBuilder = new RecordKeyBuilder();
            _columnKeyBuilder = new RecordColumnKeyBuilder();
            _sequence = new GlobalSequenceProvider(sequence);
        }

        public async Task<ulong> AddRecordAsync(Record record, byte[] secretKey = null)
        {
            return AddRecord(record, await _sequence.GetNextValueAsync());
        }

        public Task<Record> GetByIdAsync(Guid uuid)
        {
            return Task.FromResult(GetByIndex(_recordKeyBuilder.ReverseRecordKey(uuid)));
        }

        public Task<ulong> GetCountAsync(string type)
        {
            return Task.FromResult(GetCount(type));
        }

        public Task<IEnumerable<Record>> GetByColumnValueAsync(string type, string name, string value)
        {
            return Task.FromResult(GetByColumnValue(type, name, value));
        }

        public void Destroy(bool destroySequence)
        {
            Destroy();
            if (destroySequence)
                _sequence.Destroy();
        }

        public ulong GetCount(string type)
        {
            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            using var cursor = tx.CreateCursor(db);

            var key = _recordKeyBuilder.ReverseTypeKey(type);
            if (!cursor.MoveToAndGet(key))
                return 0UL;

            var count = 1UL;
            foreach (var _ in cursor)
                count++;

            return count;
        }

        private ulong AddRecord(Record record, ulong sequence)
        {
            if (record.Uuid == default)
                record.Uuid = Guid.NewGuid();

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            var context = new LogSerializeContext(bw, _typeProvider);
            record.Serialize(context, false);


            var value = ms.ToArray();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase();

            // index on key (master)
            var id = _recordKeyBuilder.BuildRecordKey(record);
            tx.Put(db, id, value, PutOptions.NoOverwrite);

            // index on type (for counts)
            var tk = _recordKeyBuilder.BuildTypeKey(record);
            tx.Put(db, tk, id, PutOptions.NoOverwrite);

            // index on column name and value (for fetch by column value)
            foreach (var column in record.Columns)
                tx.Put(db, _columnKeyBuilder.BuildColumnKey(record, column), id, PutOptions.NoOverwrite);

            tx.Commit();

            record.Index = sequence;
            return sequence + 1;
        }

        private unsafe Record GetByIndex(ReadOnlySpan<byte> index, LightningTransaction parent = null)
        {
            using var tx =
                env.Value.BeginTransaction(parent == null
                    ? TransactionBeginFlags.ReadOnly
                    : TransactionBeginFlags.None);
            using var db = tx.OpenDatabase();
            using var cursor = tx.CreateCursor(db);

            // FIXME: do not allocate here!
            if (!cursor.MoveToAndGet(index.ToArray()))
                return default;

            var found = cursor.Current;
            var buffer = found.Value.AsSpan();
            fixed (byte* buf = &buffer.GetPinnableReference())
            {
                var ms = new UnmanagedMemoryStream(buf, buffer.Length);
                var br = new BinaryReader(ms);
                var context = new LogDeserializeContext(br, _typeProvider);
                var record = new Record(context);
                return record;
            }
        }

        private IEnumerable<Record> GetByColumnValue(string type, string name, string value)
        {
            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            using var cursor = tx.CreateCursor(db);

            var key = _columnKeyBuilder.ReverseKey(type, name, value);
            if (!cursor.MoveTo(key))
                yield break;

            foreach (var idx in cursor)
            {
                var record = idx.Value.AsSpan();
                yield return GetByIndex(record, tx);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _sequence?.Dispose();
        }
    }
}