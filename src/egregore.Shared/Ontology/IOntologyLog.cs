// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace egregore.Ontology
{
    public interface IOntologyLog
    {
        long Index { get; }
        void Init(ReadOnlySpan<byte> publicKey);
        Task MaterializeAsync(ILogStore store, byte[] secretKey = default, long? startingFrom = default, CancellationToken cancellationToken = default);
        bool Exists(string eggPath);
        string GenerateModels();

        IEnumerable<Schema> GetSchemas(string ns, ulong revisionSet = 1);
        Schema GetSchema(string name, string ns, ulong revisionSet = 1);
    }
}