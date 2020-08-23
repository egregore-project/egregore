// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using egregore.Events;
using egregore.Extensions;
using LightningDB;
using Lunr;
using Microsoft.Extensions.Logging;

namespace egregore.Data
{
    internal sealed class LightningRecordStore : LightningDataStore, IRecordStore
    {
        private readonly ISequenceProvider _sequence;
        private readonly ILogObjectTypeProvider _typeProvider;
        private readonly IRecordIndex _index;
        private readonly RecordEvents _events;

        private readonly RecordColumnKeyBuilder _columnKeyBuilder;
        private readonly RecordKeyBuilder _recordKeyBuilder;

        private readonly ILogger<LightningRecordStore> _logger;

        public LightningRecordStore(string sequence, IRecordIndex index, RecordEvents events, ILogger<LightningRecordStore> logger = default)
        {
            _index = index;
            _events = events;
            _logger = logger;
            _columnKeyBuilder = new RecordColumnKeyBuilder();
            _recordKeyBuilder = new RecordKeyBuilder();

            _typeProvider = new LogObjectTypeProvider();
            _sequence = new GlobalSequenceProvider(sequence);
        }

        public async Task<ulong> AddRecordAsync(Record record, byte[] secretKey = null, CancellationToken cancellationToken = default)
        {
            var sequence = AddRecord(record, await _sequence.GetNextValueAsync(), cancellationToken);
            await _events.OnAddedAsync(this, record, cancellationToken);
            return sequence;
        }

        public Task<IEnumerable<Record>> GetByTypeAsync(string type, out ulong total, CancellationToken cancellationToken = default) => Task.FromResult(GetByType(type, out total, cancellationToken));
        public Task<Record> GetByIdAsync(Guid uuid, CancellationToken cancellationToken = default) => Task.FromResult(GetByIndex(_recordKeyBuilder.ReverseRecordKey(uuid), default, cancellationToken));
        public Task<ulong> GetLengthByTypeAsync(string type, CancellationToken cancellationToken = default) => Task.FromResult(GetLengthByType(type, cancellationToken));
        public Task<IEnumerable<Record>> GetByColumnValueAsync(string type, string name, string value, CancellationToken cancellationToken = default) => Task.FromResult(GetByColumnValue(type, name, value, cancellationToken));
        
        public IAsyncEnumerable<Record> StreamRecordsAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return AsyncEnumerableExtensions.Empty<Record>();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var startingKey = _recordKeyBuilder.AllRecordsKey();
            if (cursor.SetRange(startingKey) != MDBResultCode.Success)
                return AsyncEnumerableExtensions.Empty<Record>();

            var current = cursor.GetCurrent();
            if (current.resultCode != MDBResultCode.Success)
                return AsyncEnumerableExtensions.Empty<Record>();

            var records = new List<Record>();
            while (current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
            {
                unsafe
                {
                    var value = current.value.AsSpan();
                    var key = current.key.AsSpan();
                    var keyString = Encoding.UTF8.GetString(key);
                    if (!keyString.StartsWith("R:"))
                        break;

                    fixed (byte* buf = &value.GetPinnableReference())
                    {
                        var ms = new UnmanagedMemoryStream(buf, value.Length);
                        var br = new BinaryReader(ms);
                        var context = new LogDeserializeContext(br, _typeProvider);

                        var uuid = br.ReadGuid();
                        if (!key.SequenceEqual(_recordKeyBuilder.ReverseRecordKey(uuid)))
                            break;

                        var record = new Record(uuid, context);
                        records.Add(record);
                    }
                }

                var next = cursor.Next();
                if (next == MDBResultCode.Success)
                    current = cursor.GetCurrent();
                else
                    break;
            }

            return records.ToAsyncEnumerable(cancellationToken);
        }

        public void Destroy(bool destroySequence)
        {
            Destroy();
            if (destroySequence)
                _sequence.Destroy();
        }

        private ulong AddRecord(Record record, ulong sequence, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (record.Uuid == default)
                record.Uuid = Guid.NewGuid();

            if(record.Index == default)
                record.Index = sequence;

            if (record.TimestampV2 == default)
                record.TimestampV2 = TimestampFactory.Now.v2;

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            var context = new LogSerializeContext(bw, _typeProvider);
            record.Serialize(context, false);

            var value = ms.ToArray();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);

            // index on key (master)
            var id = _recordKeyBuilder.BuildRecordKey(record);
            tx.Put(db, id, value, PutOptions.NoOverwrite);

            // index on type (for counts and fetches by type)
            var key = _recordKeyBuilder.BuildTypeToIndexKey(record);
            tx.Put(db, key, id, PutOptions.NoOverwrite);

            // index on column name and value (for fetch by column value)
            foreach (var column in record.Columns)
                tx.Put(db, _columnKeyBuilder.BuildColumnKey(record, column), id, PutOptions.NoOverwrite);

            tx.Commit();

            return sequence;
        }

        private ulong GetLengthByType(string type, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var key = _recordKeyBuilder.ReverseTypeKey(type);
            if (cursor.SetRange(key) != MDBResultCode.Success)
                return 0UL;

            var count = 0UL;
            var current = cursor.GetCurrent();
            while (current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
            {
                count++;
                var next = cursor.Next();
                if (next == MDBResultCode.Success)
                    current = cursor.GetCurrent();
                else
                    break;
            }

            return count;
        }
        
        private IEnumerable<Record> GetByType(string type, out ulong total, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            total = GetLengthByType(type, cancellationToken);

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var results = new List<Record>();

            var key = _recordKeyBuilder.ReverseTypeKey(type);
            if (cursor.SetRange(key) != MDBResultCode.Success)
                return results;
            
            var current = cursor.GetCurrent();
            while (current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
            {
                var index = current.value.AsSpan();
                var record = GetByIndex(index, tx, cancellationToken);
                if (record == default)
                    break;

                results.Add(record);

                var next = cursor.Next();
                if (next == MDBResultCode.Success)
                    current = cursor.GetCurrent();
                else
                    break;
            }

            return results;
        }

        private unsafe Record GetByIndex(ReadOnlySpan<byte> index, LightningTransaction parent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(parent == null ? TransactionBeginFlags.ReadOnly : TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var (sr, _, _) = cursor.SetKey(index);
            if (sr != MDBResultCode.Success)
                return default;

            var (gr, _, value) = cursor.GetCurrent();
            if (gr != MDBResultCode.Success)
                return default;

            var buffer = value.AsSpan();

            fixed (byte* buf = &buffer.GetPinnableReference())
            {
                var ms = new UnmanagedMemoryStream(buf, buffer.Length);
                var br = new BinaryReader(ms);
                var context = new LogDeserializeContext(br, _typeProvider);

                var uuid = br.ReadGuid();
                if (!index.SequenceEqual(_recordKeyBuilder.ReverseRecordKey(uuid)))
                    return default;

                var record = new Record(uuid, context);
                return record;
            }
        }

        private IEnumerable<Record> GetByColumnValue(string type, string name, string value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var results = new List<Record>();

            var key = _columnKeyBuilder.ReverseKey(type, name, value); // FIXME: don't allocate
            if (cursor.SetKey(key).resultCode != MDBResultCode.Success)
                return results;

            var current = cursor.GetCurrent();
            while(current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
            {
                var record = GetByIndex(current.value.AsSpan(), tx, cancellationToken);
                if (record == default)
                    break;

                results.Add(record);

                var next = cursor.Next();
                if (next == MDBResultCode.Success)
                    current = cursor.GetCurrent();
                else
                    break;
            }
            return results;
        }

        public async IAsyncEnumerable<Record> SearchAsync(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            await foreach (var result in _index.SearchAsync(query, cancellationToken).WithCancellation(cancellationToken))
            {
                if (Guid.TryParse(result.DocumentReference, out var uuid))
                {
                    yield return await GetByIdAsync(uuid, cancellationToken);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            _sequence?.Dispose();
        }
    }
}