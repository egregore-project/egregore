// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using egregore.Configuration;
using Microsoft.Extensions.Options;

namespace egregore.Ontology
{
    public class OntologyWriter
    {
        private readonly ILogStore _store;
        private readonly ILogObjectTypeProvider _typeProvider;
        private readonly ILogEntryHashProvider _hashProvider;
        private readonly IOntologyChangeHandler _changeHandler;
        private readonly IOptions<WebServerOptions> _options;

        public OntologyWriter(ILogStore store, ILogObjectTypeProvider typeProvider, ILogEntryHashProvider hashProvider, IOntologyChangeHandler changeHandler, IOptions<WebServerOptions> options)
        {
            _store = store;
            _typeProvider = typeProvider;
            _hashProvider = hashProvider;
            _changeHandler = changeHandler;
            _options = options;
        }

        public async Task<T> SaveAsync<T>(T model) where T : ILogSerialized
        {
            _store.Init(_options.Value.EggPath);
            var index = await _store.AddEntryAsync(Wrap(model));
            _changeHandler.OnOntologyChanged((long) index);
            return model;
        }

        internal LogEntry Wrap<T>(T model) where T : ILogSerialized
        {
            var type = _typeProvider.Get(typeof(Page)).GetValueOrDefault();
            var version = LogSerializeContext.FormatVersion;

            var @object = new LogObject
            {
                Timestamp = TimestampFactory.Now,
                Type = type,
                Version = version,
                Data = model
            };

            @object.Hash = _hashProvider.ComputeHashBytes(@object);

            // FIXME: inefficient, cache the last value somewhere?
            var previousHash = new byte[0];
            foreach(var item in _store.StreamEntries())
                previousHash = item.Hash;
            
            var entry = new LogEntry
            {
                PreviousHash = previousHash,
                Timestamp = @object.Timestamp,
                Nonce = Crypto.Nonce(64U),
                Objects = new[] {@object}
            };

            entry.HashRoot = _hashProvider.ComputeHashRootBytes(entry);
            entry.Hash = _hashProvider.ComputeHashBytes(entry);

            return entry;
        }
    }
}