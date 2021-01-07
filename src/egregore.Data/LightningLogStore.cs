// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LightningDB;

namespace egregore.Data
{
    public sealed class LightningLogStore : LightningDataStore, ILogStore
    {
        private readonly ILogEntryHashProvider _hashProvider;
        private readonly SequenceKeyBuilder _keyBuilder;
        private readonly ISequenceProvider _sequence;
        private readonly ILogObjectTypeProvider _typeProvider;

        public LightningLogStore(ILogObjectTypeProvider typeProvider)
        {
            _typeProvider = typeProvider;
            _hashProvider = new LogEntryHashProvider(_typeProvider);
            _keyBuilder = new SequenceKeyBuilder();
            _sequence = new LogStoreSequenceProvider(this);
        }

        public async Task<ulong> AddEntryAsync(LogEntry entry, byte[] secretKey = null,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return 0UL;

            entry.RoundTripCheck(_typeProvider, secretKey);

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            entry.Serialize(new LogSerializeContext(bw, _typeProvider), false);
            var value = ms.ToArray();

            var index = await _sequence.GetNextValueAsync();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase();

            var key = _keyBuilder.BuildKey(index, entry);
            if (key.Length >= MaxKeySizeBytes)
                throw new InvalidOperationException($"Keys must be less than {MaxKeySizeBytes} bytes in length.");

            tx.Put(db, key, value);
            tx.Commit();

            entry.Index = index;
            return index;
        }

        public IEnumerable<LogEntry> StreamEntries(ulong startingFrom = 0, byte[] secretKey = null,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            LogEntry previousEntry = default;

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();

            var length = tx.GetEntriesCount(db);
            while (startingFrom < (ulong) length && !cancellationToken.IsCancellationRequested)
            {
                var key = BitConverter.GetBytes((long) startingFrom);

                var (r, _, v) = tx.Get(db, key);
                if (r != MDBResultCode.Success)
                    yield break;

                using var ms = new MemoryStream(v.AsSpan().ToArray());
                using var br = new BinaryReader(ms);
                var context = new LogDeserializeContext(br, _typeProvider);

                var entry = new LogEntry(context)
                {
                    Index = startingFrom++
                };

                if (previousEntry != default)
                    entry.EntryCheck(previousEntry, _hashProvider);

                yield return entry;
                previousEntry = entry;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _sequence.Dispose();
            base.Dispose(disposing);
        }
    }
}