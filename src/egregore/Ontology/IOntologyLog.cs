// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using egregore.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace egregore.Ontology
{
    public interface IOntologyLog
    {
        long Index { get; }
        void Init(ReadOnlySpan<byte> publicKey);
        void Materialize(ILogStore store, IHubContext<NotificationHub> hub, OntologyChangeProvider change, byte[] secretKey = default, long? startingFrom = default);
        bool Exists(string eggPath);
    }
}