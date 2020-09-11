// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using egregore.Data;
using egregore.Extensions;
using LightningDB;
using Microsoft.Extensions.Logging;

namespace egregore.Logging
{
    public sealed class LightningLoggingStore : LightningDataStore
    {
#pragma warning disable CS0169 // The field 'LightningLoggingStore._index' is never used
        private Lunr.Index _index;
#pragma warning restore CS0169 // The field 'LightningLoggingStore._index' is never used

        public LightningLoggingStore()
        {
            
        }

        public void Append<TState>(LogLevel logLevel, in EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var entry = new LoggingEntry(Guid.NewGuid(), exception)
            {
                LogLevel = logLevel, 
                EventId = eventId, 
                Message = formatter(state, exception)
            };

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            var context = new LoggingSerializeContext(bw);
            entry.Serialize(context);
            
            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);
            
            var id = entry.Id.ToByteArray();
            var value = ms.ToArray();

            // master
            tx.Put(db, id, value, PutOptions.NoOverwrite);
            
            // entry-by-id => master
            tx.Put(db, Encoding.UTF8.GetBytes($"E:{entry.Id}"), id, PutOptions.NoOverwrite);

            // get by level
            tx.Put(db, Encoding.UTF8.GetBytes($"L:{entry.LogLevel}:{entry.Id}"), id, PutOptions.NoOverwrite);

            tx.Commit();
        }
        
        public IEnumerable<LoggingEntry> Get(CancellationToken cancellationToken = default)
        {
            var key = Encoding.UTF8.GetBytes("E:");
            return GetByChildKey(key, cancellationToken);
        }

        public IEnumerable<LoggingEntry> GetByLevel(LogLevel logLevel, CancellationToken cancellationToken = default)
        {
            var key = Encoding.UTF8.GetBytes($"L:{logLevel}");
            return GetByChildKey(key, cancellationToken);
        }

        private IEnumerable<LoggingEntry> GetByChildKey(ReadOnlySpan<byte> key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var entries = new List<LoggingEntry>();
            if (cursor.SetRange(key) != MDBResultCode.Success)
                return entries;

            var current = cursor.GetCurrent();
            while (current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
            {
                var index = current.value.AsSpan();
                var entry = GetByIndex(index, tx, cancellationToken);
                if (entry == default)
                    break;

                entries.Add(entry);

                var next = cursor.Next();
                if (next == MDBResultCode.Success)
                    current = cursor.GetCurrent();
                else
                    break;
            }

            return entries;
        }
        
        private unsafe LoggingEntry GetByIndex(ReadOnlySpan<byte> index, LightningTransaction parent, CancellationToken cancellationToken)
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
                using var ms = new UnmanagedMemoryStream(buf, buffer.Length);
                using var br = new BinaryReader(ms);
                var context = new LoggingDeserializeContext(br);
                
                var uuid = br.ReadGuid();
                var entry = new LoggingEntry(uuid, context);
                return entry;
            }
        }
    }
}