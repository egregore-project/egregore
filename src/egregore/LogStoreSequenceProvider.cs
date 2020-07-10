// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using egregore.Ontology;

namespace egregore
{
    internal sealed class LogStoreSequenceProvider : ISequenceProvider
    {
        private readonly ILogStore _store;

        public LogStoreSequenceProvider(ILogStore store)
        {
            _store = store;
        }

        public async Task<ulong> GetNextValueAsync()
        {
            return await _store.GetLengthAsync();
        }

        public void Destroy()
        {
            _store.Destroy();
        }

        public void Dispose() { }
    }
}