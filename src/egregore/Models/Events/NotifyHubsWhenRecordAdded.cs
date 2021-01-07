// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using egregore.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace egregore.Models.Events
{
    internal sealed class NotifyHubsWhenRecordAdded : RecordEventHandler
    {
        private readonly IHubContext<NotificationHub> _notify;
        private readonly IHubContext<LiveQueryHub> _queries;

        public NotifyHubsWhenRecordAdded(IHubContext<NotificationHub> notify, IHubContext<LiveQueryHub> queries)
        {
            _notify = notify;
            _queries = queries;
        }

        public override Task OnRecordAddedAsync(IRecordStore store, Record record,
            CancellationToken cancellationToken = default)
        {
            var pending = AsyncExtensions.TaskPool.Get();
            try
            {
                var notify = _notify.Clients.All.SendAsync(Constants.Notifications.ReceiveMessage, "info",
                    $"Added new record with ID '{record.Uuid}'", cancellationToken);
                if (notify.IsCompleted || notify.IsCanceled || notify.IsFaulted)
                    return AsyncExtensions.NoTask;

                if (notify.Status != TaskStatus.Running)
                    notify.Start();

                pending.Add(notify);
                return Task.WhenAll(pending);
            }
            finally
            {
                AsyncExtensions.TaskPool.Return(pending);
            }
        }
    }
}