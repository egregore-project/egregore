// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using egregore.Extensions;
using Microsoft.Data.Sqlite;

namespace egregore
{
    internal sealed class LogStore : ILogStore
    {
        public sealed class UInt64TypeHandler : SqlMapper.TypeHandler<ulong?>
        {
            public override void SetValue(IDbDataParameter parameter, ulong? value) => parameter.Value = value;
            public override ulong? Parse(object value) => value is long v ? (ulong) v : !ulong.TryParse((string) value, out var val) ? default(ulong?) : val;
        }

        public sealed class UInt128TypeHandler : SqlMapper.TypeHandler<UInt128?>
        {
            public override void SetValue(IDbDataParameter parameter, UInt128? value) => parameter.Value = value.GetValueOrDefault().ToString();
            public override UInt128? Parse(object value) => value is UInt128 v ? v : new UInt128((string)value);
        }

        static LogStore()
        {
            SqlMapper.AddTypeHandler(new UInt64TypeHandler());
            SqlMapper.AddTypeHandler(new UInt128TypeHandler());
        }

        private readonly string _filePath;
        private readonly ILogObjectTypeProvider _typeProvider;
        private readonly HashProvider _hashProvider;

        public string DataFile { get; private set; }

        public void Init()
        {
            CreateIfNotExists(_filePath);
            MigrateToLatest(_filePath);
        }

        internal LogStore(string filePath)
        {
            _filePath = filePath;
            _typeProvider = new LogObjectTypeProvider();
            _hashProvider = new HashProvider(_typeProvider);
        }

        public async Task<ulong> GetLengthAsync()
        {
            await using var db = new SqliteConnection($"Data Source={DataFile}");
            const string sql = "SELECT MAX(b.'Index') FROM 'LogEntry' b";
            return (await db.QuerySingleOrDefaultAsync<ulong?>(sql)).GetValueOrDefault(0UL);
        }

        public async Task<ulong> AddEntryAsync(LogEntry entry, byte[] secretKey)
        {
            var data = SerializeObjects(entry, secretKey);

            await using var db = new SqliteConnection($"Data Source={DataFile}");
            await db.OpenAsync();
            
            await using var t = await db.BeginTransactionAsync();

            var index = db.QuerySingle<ulong>(
                "INSERT INTO 'LogEntry' " +
                "('Version','PreviousHash','HashRoot','Timestamp','Nonce','Hash','Data') VALUES" +
                "(:Version,:PreviousHash,:HashRoot,:Timestamp,:Nonce,:Hash,:Data); " +
                "SELECT LAST_INSERT_ROWID();", new
                {
                    entry.Version,
                    entry.PreviousHash,
                    entry.HashRoot,
                    entry.Timestamp,
                    entry.Nonce,
                    entry.Hash,
                    Data = data
                }, t);

            t.Commit();

            entry.Index = index;
            return index;
        }

        public IEnumerable<LogEntry> StreamEntries(ulong startingFrom, byte[] secretKey = null)
        {
            using var db = new SqliteConnection($"Data Source={DataFile}");

            const string sql = "SELECT e.* " +
                               "FROM 'LogEntry' e " +
                               "WHERE e.'Index' >= @startingFrom " +
                               "ORDER BY e.'Index' ASC";

            var stream = db.Query<LogEntryWithData>(sql, new { startingFrom }, buffered: false);

            // ReSharper disable once PossibleMultipleEnumeration
            var previousEntry = stream?.Take(1).FirstOrDefault();
            if (previousEntry == default)
                yield break;

            DeserializeObjects(previousEntry, previousEntry.Data, secretKey);
            previousEntry.Data = null;
            yield return previousEntry;

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var entry in stream.Skip(1))
            {
                DeserializeObjects(entry, entry.Data, secretKey);
                entry.Data = null;
                EntryCheck(previousEntry, entry);
                yield return entry;
                previousEntry = entry;
            }
        }

        private void EntryCheck(LogEntry previous, LogEntry current)
        {
            if (previous.Index + 1 != current.Index)
            {
                var message = $"Invalid index: expected '{previous.Index + 1}' but was '{current.Index}'";
                throw new LogException(message);
            }

            if (!previous.Hash.SequenceEqual(current.PreviousHash))
            {
                var message = $"Invalid previous hash: expected '{previous.Hash}' but was '{current.PreviousHash}'";
                throw new LogException(message);
            }

            var hashRoot = _hashProvider.ComputeHashRootBytes(current);
            if (!hashRoot.SequenceEqual(current.HashRoot))
            {
                var message = $"Invalid hash root: expected '{Crypto.ToHexString(hashRoot)}' but was '{Crypto.ToHexString(current.HashRoot)}'";
                throw new LogException(message);
            }

            var hash = _hashProvider.ComputeHashBytes(current);
            if (!hash.SequenceEqual(current.Hash))
            {
                var message = $"Invalid hash: expected '{Crypto.ToHexString(hash)}' but was '{Crypto.ToHexString(current.Hash)}'";
                throw new LogException(message);
            }
        }

        public sealed class LogEntryWithData : LogEntry
        {
            public byte[] Data { get; set; }
        }

        #region Serialization

        private byte[] SerializeObjects(LogEntry entry, byte[] secretKey = default)
        {
            entry.RoundTripCheck(_typeProvider, secretKey);

            byte[] data;
            using (var ms = new MemoryStream())
            {
                using var bw = new BinaryWriter(ms, Encoding.UTF8);

                // Version:
                var context = new LogSerializeContext(bw, _typeProvider);

                if (secretKey != null)
                {
                    // Nonce:
                    var nonce = SecretStream.Nonce();
                    context.bw.WriteVarBuffer(nonce);

                    // Data:
                    using var ems = new MemoryStream();
                    using var ebw = new BinaryWriter(ems, Encoding.UTF8);
                    var ec = new LogSerializeContext(ebw, _typeProvider, context.Version);
                    entry.SerializeObjects(ec, false);
                    
                    var message = SecretStream.EncryptMessage(ems.ToArray(), nonce, secretKey);
                    context.bw.WriteVarBuffer(message);
                }
                else
                {
                    // Data:
                    context.bw.Write(false);
                    entry.SerializeObjects(context, false);
                }

                data = ms.ToArray();
            }

            return data;
        }

        private void DeserializeObjects(LogEntry entry, byte[] data, byte[] secretKey)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);

            // Version:
            var context = new LogDeserializeContext(br, _typeProvider);

            // Nonce:
            var nonce = context.br.ReadVarBuffer();
            if(nonce != null)
            {
                var message = SecretStream.DecryptMessage(context.br.ReadVarBuffer(), nonce, secretKey);

                // Data:
                using var dms = new MemoryStream(message);
                using var dbr = new BinaryReader(dms);
                var dc = new LogDeserializeContext(dbr, _typeProvider);
                entry.DeserializeObjects(dc);
            }
            else
            {
                // Data:
                entry.DeserializeObjects(context);
            }
        }

        #endregion

        #region Migration

        private void CreateIfNotExists(string filePath)
        {
            var baseDirectory = Path.GetDirectoryName(filePath);
            if(!string.IsNullOrWhiteSpace(baseDirectory))
                Directory.CreateDirectory(baseDirectory);
            MigrateToLatest(_filePath);
            DataFile = _filePath;
        }

        private static void MigrateToLatest(string filePath)
        {
            try
            {
                var db = new SqliteConnection($"Data Source={filePath}");
                db.Open();

                db.Execute(@"CREATE TABLE IF NOT EXISTS 'LogEntry'
(  
    'Index' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	'Version' INTEGER NOT NULL,
    'PreviousHash' VARCHAR(64) NOT NULL, 
	'HashRoot' VARCHAR(64) NOT NULL,
    'Timestamp' INTEGER NOT NULL,
	'Nonce' INTEGER NOT NULL,
    'Hash' VARCHAR(64) UNIQUE NOT NULL,
	'Data' BLOB NOT NULL
);");

                db.Close();
                db.Dispose();
            }
            catch (SqliteException e)						 
            {
                Trace.TraceError("Error migrating log store: {0}", e);
                throw;
            }
        }

        #endregion
    }
}