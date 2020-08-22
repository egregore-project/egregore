// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lunr;
using Microsoft.Extensions.Logging;
using Index = Lunr.Index;

namespace egregore.Data
{
    internal sealed class LunrRecordIndex : IRecordIndex
    {
        private Index _index;

        private readonly IRecordStore _store;
        private readonly ILogger<LunrRecordIndex> _logger;

        public LunrRecordIndex(IRecordStore store, ILogger<LunrRecordIndex> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async IAsyncEnumerable<RecordSearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var result in _index.Search(query, cancellationToken).WithCancellation(cancellationToken))
            {
                if (Guid.TryParse(result.DocumentReference, out _))
                {
                    yield return new RecordSearchResult { DocumentReference = result.DocumentReference};
                }
            }
        }

        public async Task RebuildAsync()
        {
            _index = await Index.Build(async builder =>
            {
                var fields = new HashSet<string>();
                var sw = Stopwatch.StartNew();
                var count = 0UL;

                await foreach (var entry in _store.StreamRecordsAsync(default))
                {
                    foreach (var column in entry.Columns)
                    {
                        if (fields.Contains(column.Name))
                            continue;
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
    }
}