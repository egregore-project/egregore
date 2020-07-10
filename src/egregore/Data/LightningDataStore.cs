// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LightningDB;

namespace egregore.Data
{
    public abstract class LightningDataStore : IDisposable
    {
        protected const ushort MaxKeySizeBytes = 511;

        private const int DefaultMaxReaders = 126;
        private const int DefaultMaxDatabases = 5;
        private const long DefaultMapSize = 10_485_760;

        public string DataFile { get; private set; }

        protected Lazy<LightningEnvironment> env;

        protected LightningDataStore(string path)
        {
            env = new Lazy<LightningEnvironment>(() =>
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
            if (env.IsValueCreated)
                return;
            DataFile = env.Value.Path;
        }

        private static void CreateIfNotExists(LightningEnvironment environment)
        {
            using var tx = environment.BeginTransaction();
            try
            {
                using (tx.OpenDatabase(null, new DatabaseConfiguration()))
                    tx.Commit();
            }
            catch (LightningException)
            {
                using (tx.OpenDatabase(null, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
                    tx.Commit();
            }
        }

        public Task<ulong> GetLengthAsync()
        {
            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            var count = (ulong) tx.GetEntriesCount(db); // entries also contains handles to databases
            return Task.FromResult(count);
        }
        
        public void Destroy()
        {
            using (var tx = env.Value.BeginTransaction())
            {
                var db = tx.OpenDatabase();
                tx.DropDatabase(db);
                tx.Commit();
            }
            env.Value.Dispose();
            Directory.Delete(env.Value.Path, true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            if(env.IsValueCreated)
                env.Value.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}