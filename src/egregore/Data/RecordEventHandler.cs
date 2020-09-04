// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;
using System.Threading.Tasks;
using egregore.Extensions;

namespace egregore.Data
{
    internal abstract class RecordEventHandler : IRecordEventHandler
    {
        public Task OnRecordsInitAsync(IRecordStore store, CancellationToken cancellationToken = default)
        {
            return AsyncExtensions.NoTask;
        }

        public abstract Task OnRecordAddedAsync(IRecordStore store, Record record,
            CancellationToken cancellationToken = default);
    }
}