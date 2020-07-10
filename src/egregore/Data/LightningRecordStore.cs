// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LightningDB;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;

namespace egregore.Data
{
    internal sealed class LightningRecordStore : LightningDataStore, IRecordStore
    {
        private readonly ILogObjectTypeProvider _typeProvider;
        private readonly ISequenceProvider _sequence;

        private readonly RecordKeyBuilder _recordKeyBuilder;
        private readonly RecordColumnKeyBuilder _columnKeyBuilder;

        public LightningRecordStore(string path, string sequence = Constants.DefaultSequence) : base(path)
        {
            _typeProvider = new LogObjectTypeProvider();
            _recordKeyBuilder = new RecordKeyBuilder();
            _columnKeyBuilder = new RecordColumnKeyBuilder();
            _sequence = new GlobalSequenceProvider(sequence);
        }

        public async Task<ulong> AddRecordAsync(Record record, byte[] secretKey = null) => AddRecord(record, await _sequence.GetNextValueAsync());
        public Task<Record> GetByIdAsync(Guid uuid) => Task.FromResult(GetByIndex(_recordKeyBuilder.ReverseRecordKey(uuid)));
        public Task<ulong> GetCountAsync(string type) => Task.FromResult(GetCount(type));
        public Task<IEnumerable<Record>> GetByColumnValueAsync(string type, string name, string value) => Task.FromResult(GetByColumnValue(type, name, value));

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
            using var tx = env.Value.BeginTransaction(parent == null ? TransactionBeginFlags.ReadOnly : TransactionBeginFlags.None);
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