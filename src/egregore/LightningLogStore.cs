// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LightningDB;

namespace egregore
{
    public sealed class LightningLogStore : ILogStore, IDisposable
    {
        private const ushort MaxKeySize = 511;

        private const int DefaultMaxReaders = 126;
        private const int DefaultMaxDatabases = 5;
        private const long DefaultMapSize = 10_485_760;

        public string DataFile { get; private set; }

        private readonly Lazy<LightningEnvironment> _environment;
        private readonly ILogObjectTypeProvider _typeProvider;
        private readonly HashProvider _hashProvider;

        public LightningLogStore(string path)
        {
            _typeProvider = new LogObjectTypeProvider();
            _hashProvider = new HashProvider(_typeProvider);

            _environment = new Lazy<LightningEnvironment>(() =>
            {
                var config = new EnvironmentConfiguration
                {
                    MaxDatabases = DefaultMaxDatabases, 
                    MaxReaders = DefaultMaxReaders,
                    MapSize = DefaultMapSize
                };
                var environment = new LightningEnvironment(path, config);
                environment.Open();
                CreateIfNotExists(environment);
                return environment;
            });
        }

        public void Init()
        {
            if (_environment.IsValueCreated)
                return;
            DataFile = _environment.Value.Path;
        }

        public Task<ulong> GetLengthAsync()
        {
            using var tx = _environment.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            var count = (ulong) tx.GetEntriesCount(db);
            return Task.FromResult(count - 1);
        }

        public Task<ulong> AddEntryAsync(LogEntry entry, byte[] secretKey = null)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            entry.Serialize(new LogSerializeContext(bw, _typeProvider), false);
            var value = ms.ToArray();

            using var tx = _environment.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase();
            
            var index = (ulong)(tx.GetEntriesCount(db) - 1);
            var key = BitConverter.GetBytes(index);
            Debug.Assert(key.Length < MaxKeySize);

            tx.Put(db, key, value);
            tx.Commit();

            entry.Index = index;
            return Task.FromResult(index + 1);
        }

        public IEnumerable<LogEntry> StreamEntries(ulong startingFrom = 0, byte[] secretKey = null)
        {
            LogEntry previousEntry = default;

            using var tx = _environment.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();

            var count = (ulong) (tx.GetEntriesCount(db) - 1 - (long) startingFrom);

            while (startingFrom < count)
            {
                var key = BitConverter.GetBytes((long) startingFrom);
                var value = tx.Get(db, key);
                if (value == default)
                    yield break;

                using var ms = new MemoryStream(value);
                using var br = new BinaryReader(ms);
                var context = new LogDeserializeContext(br, _typeProvider);
                var entry = new LogEntry(context);
                
                if (previousEntry != default)
                    entry.EntryCheck(previousEntry, _hashProvider);
                yield return entry;
                previousEntry = entry;
                startingFrom++;
            }
        }

        public void Purge()
        {
            using (var tx = _environment.Value.BeginTransaction())
            {
                var db = tx.OpenDatabase();
                tx.DropDatabase(db);
                tx.Commit();
            }
            _environment.Value.Dispose();
            Directory.Delete(_environment.Value.Path, true);
        }

        private static void CreateIfNotExists(LightningEnvironment environment)
        {
            using var tx = environment.BeginTransaction();
            try
            {
                using (tx.OpenDatabase("db", new DatabaseConfiguration()))
                    tx.Commit();
            }
            catch (LightningException)
            {
                using (tx.OpenDatabase("db", new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
                    tx.Commit();
            }
        }

        public void Dispose()
        {
            if(_environment.IsValueCreated)
                _environment.Value.Dispose();
        }
    }
}