using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using egregore.Ontology;
using LightningDB;

namespace egregore.Pages
{
    public class LightningPageStore : LightningDataStore, IPageStore
    {
        private static readonly ImmutableList<Page> NoEntries = new List<Page>(0).ToImmutableList();

        private readonly PageEvents _events;
        private readonly PageKeyBuilder _pageKeyBuilder;
        private readonly ILogObjectTypeProvider _typeProvider;

        public LightningPageStore(PageEvents events, ILogObjectTypeProvider typeProvider)
        {
            _events = events;
            _typeProvider = typeProvider;
            _pageKeyBuilder = new PageKeyBuilder();
        }

        public Task<IEnumerable<Page>> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Get(cancellationToken), cancellationToken);
        }

        public Task<Page> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => GetById(id), cancellationToken);
        }

        public Task AddPageAsync(Page page, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.Run(async ()=>
            {
                AddPage(page);
                await _events.OnPageAddedAsync(this, page, cancellationToken);
            }, cancellationToken);
        }

        private void AddPage(Page page)
        {
            if (page.Uuid == default)
                page.Uuid = Guid.NewGuid();

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            page.Serialize(new LogSerializeContext(bw, _typeProvider), false);
            var value = ms.ToArray();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);

            var key = _pageKeyBuilder.GetKey(page);
            tx.Put(db, key, value, PutOptions.NoOverwrite);

            // index by page name

            // index by hashes (check-sums, etc.)

            // full text search on title, plaintext body, and tags
            
            var result = tx.Commit();

            if (result != MDBResultCode.Success)
                throw new InvalidOperationException();
        }

        private IEnumerable<Page> Get(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var results = new List<Page>();

            var key = _pageKeyBuilder.GetAllKey();
            if (cursor.SetRange(key) != MDBResultCode.Success)
                return NoEntries;

            var current = cursor.GetCurrent();
            while (current.resultCode == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
            {
                unsafe
                {
                    var value = current.value.AsSpan();
                    fixed (byte* buf = &value.GetPinnableReference())
                    {
                        var ms = new UnmanagedMemoryStream(buf, value.Length);
                        var br = new BinaryReader(ms);
                        var context = new LogDeserializeContext(br, _typeProvider);

                        var page = new Page(context);
                        results.Add(page);
                    }
                    
                    var next = cursor.Next();
                    if (next == MDBResultCode.Success)
                        current = cursor.GetCurrent();
                    else
                        break;
                }
            }

            return results;
        }

        private Page GetById(Guid id)
        {
            unsafe
            {
                using var tx = env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
                using var db = tx.OpenDatabase(configuration: Config);
                using var cursor = tx.CreateCursor(db);

                var key = _pageKeyBuilder.GetKey(id);
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
                    var entry = new Page(context);
                    return entry;
                }
            }
        }
    }
}
