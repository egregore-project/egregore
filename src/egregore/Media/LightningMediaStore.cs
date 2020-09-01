using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using LightningDB;

namespace egregore.Media
{
    public class LightningMediaStore : LightningDataStore, IMediaStore
    {
        public Task<IEnumerable<MediaEntry>> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var results = new List<MediaEntry>();

            var key = Encoding.UTF8.GetBytes("M:");
            if (cursor.SetRange(key) != MDBResultCode.Success)
                return Task.FromResult((IEnumerable<MediaEntry>)results);
            
            var current = cursor.GetCurrent();
            while (current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
            {
                var idString = Encoding.UTF8.GetString(current.key.AsSpan());
                var id = Guid.Parse(idString.Substring(2));

                var entry = new MediaEntry();
                entry.Uuid = id;
                entry.Data = current.value.AsSpan().ToArray();
                results.Add(entry);

                var next = cursor.Next();
                if (next == MDBResultCode.Success)
                    current = cursor.GetCurrent();
                else
                    break;
            }

            return Task.FromResult((IEnumerable<MediaEntry>)results);
        }

        public Task<byte[]> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var key = GetKey(id);
            var (sr, _, _) = cursor.SetKey(key);
            if (sr != MDBResultCode.Success)
                return default;

            var (gr, _, value) = cursor.GetCurrent();
            if (gr != MDBResultCode.Success)
                return default;

            var buffer = value.AsSpan().ToArray();
            return Task.FromResult(buffer);
        }

        public Task AddMediaAsync(MediaEntry media, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (media.Uuid == default)
                media.Uuid = Guid.NewGuid();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);

            var key = GetKey(media);
            var value = media.Data;
            tx.Put(db, key, value, PutOptions.NoOverwrite);

            var result = tx.Commit();

            if (result != MDBResultCode.Success)
                throw new InvalidOperationException();

            return Task.CompletedTask;
        }

        private static byte[] GetKey(MediaEntry media)
        {
            return Encoding.UTF8.GetBytes($"M:{media.Uuid}");
        }

        private static byte[] GetKey(Guid id)
        {
            return Encoding.UTF8.GetBytes($"M:{id}");
        }
    }
}
