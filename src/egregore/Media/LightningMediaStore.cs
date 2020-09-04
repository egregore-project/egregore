// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using LightningDB;

namespace egregore.Media
{
    public class LightningMediaStore : LightningDataStore, IMediaStore
    {
        private static readonly ImmutableList<MediaEntry> NoEntries = new List<MediaEntry>(0).ToImmutableList();

        private readonly MediaEvents _events;
        private readonly MediaKeyBuilder _mediaKeyBuilder;
        private readonly ILogObjectTypeProvider _typeProvider;

        public LightningMediaStore(MediaEvents events, ILogObjectTypeProvider typeProvider)
        {
            _events = events;
            _typeProvider = typeProvider;
            _mediaKeyBuilder = new MediaKeyBuilder();
        }

        public Task<IEnumerable<MediaEntry>> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Get(cancellationToken), cancellationToken);
        }

        public Task<MediaEntry> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GetById(id), cancellationToken);
        }

        public Task AddMediaAsync(MediaEntry media, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.Run(async () =>
            {
                AddMedia(media);
                await _events.OnAddedAsync(this, media, cancellationToken);
            }, cancellationToken);
        }

        private void AddMedia(MediaEntry media)
        {
            if (media.Uuid == default)
                media.Uuid = Guid.NewGuid();

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            media.Serialize(new LogSerializeContext(bw, _typeProvider), false);
            var value = ms.ToArray();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);

            var key = _mediaKeyBuilder.GetKey(media);
            tx.Put(db, key, value, PutOptions.NoOverwrite);

            // index by media type

            // index by hashes (check-sums, spectral, etc.)

            // full text search on name and meta-data

            // allow only streaming out the header data, via MediaEntryHeader

            // allow ranged requests for streaming

            var result = tx.Commit();

            if (result != MDBResultCode.Success)
                throw new InvalidOperationException();
        }

        private IEnumerable<MediaEntry> Get(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var results = new List<MediaEntry>();

            var key = _mediaKeyBuilder.GetAllKey();
            if (cursor.SetRange(key) != MDBResultCode.Success)
                return NoEntries;

            var current = cursor.GetCurrent();
            while (current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
                unsafe
                {
                    var value = current.value.AsSpan();
                    fixed (byte* buf = &value.GetPinnableReference())
                    {
                        var ms = new UnmanagedMemoryStream(buf, value.Length);
                        var br = new BinaryReader(ms);
                        var context = new LogDeserializeContext(br, _typeProvider);

                        var entry = new MediaEntry(context);
                        results.Add(entry);
                    }

                    var next = cursor.Next();
                    if (next == MDBResultCode.Success)
                        current = cursor.GetCurrent();
                    else
                        break;
                }

            return results;
        }

        private MediaEntry GetById(Guid id)
        {
            unsafe
            {
                using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
                using var db = tx.OpenDatabase(configuration: Config);
                using var cursor = tx.CreateCursor(db);

                var key = _mediaKeyBuilder.GetKey(id);
                var (sr, _, _) = cursor.SetKey(key);
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
                    var entry = new MediaEntry(context);
                    return entry;
                }
            }
        }
    }
}