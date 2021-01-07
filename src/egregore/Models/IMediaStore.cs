// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using egregore.Media;

namespace egregore.Models
{
    public interface IMediaStore : IDataStore
    {
        Task<IEnumerable<MediaEntry>> GetAsync(CancellationToken cancellationToken = default);
        Task<MediaEntry> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddMediaAsync(MediaEntry media, CancellationToken cancellationToken = default);
    }
}