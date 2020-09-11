// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using Lunr;
using Microsoft.Extensions.Logging;
using Index = Lunr.Index;

namespace egregore.Search
{
    internal sealed class LunrRecordIndex : ISearchIndex
    {
        private readonly ILogger<LunrRecordIndex> _logger;
        private Index _index;

        public LunrRecordIndex(ILogger<LunrRecordIndex> logger)
        {
            _logger = logger;
        }

        public async Task RebuildAsync(IRecordStore store, CancellationToken cancellationToken = default)
        {
            _index = await Index.Build(async builder =>
            {
                var fields = new HashSet<string>();
                var sw = Stopwatch.StartNew();
                var count = 0UL;

                await foreach (var entry in store.StreamRecordsAsync(cancellationToken))
                {
                    foreach (var column in entry.Columns.Where(column => !fields.Contains(column.Name)))
                    {
                        builder.AddField(column.Name);
                        fields.Add(column.Name);
                    }

                    var document = new Document {{"id", entry.Uuid}};
                    foreach (var column in entry.Columns)
                        document.Add(column.Name, column.Value);

                    await builder.Add(document);
                    count++;
                }

                _logger?.LogInformation($"Indexing {count} documents took {sw.Elapsed.TotalMilliseconds}ms");
            });
        }

        public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var result in _index.Search(query, cancellationToken).WithCancellation(cancellationToken))
                if (Guid.TryParse(result.DocumentReference, out _))
                    yield return new SearchResult {DocumentReference = result.DocumentReference};
        }
    }
}