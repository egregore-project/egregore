// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
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

        public Task<Record> GetByIdAsync(Guid uuid)
        {
            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            using var cursor = tx.CreateCursor(db);
            
            if(!cursor.MoveTo(uuid.ToByteArray()))
                return Task.FromResult(default(Record));

            var found = cursor.Current;

            unsafe
            {
                var buffer = found.Value.AsSpan();
                fixed (byte* buf = &buffer.GetPinnableReference())
                {
                    var ms = new UnmanagedMemoryStream(buf, buffer.Length);
                    var br = new BinaryReader(ms);
                    var context = new LogDeserializeContext(br, _typeProvider);
                    var record = new Record(context);
                    return Task.FromResult(record);
                }
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