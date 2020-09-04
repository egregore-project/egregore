// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;
using System.Threading.Tasks;
using egregore.Extensions;
using egregore.Hubs;
using egregore.Ontology;
using egregore.Pages;
using Microsoft.AspNetCore.SignalR;

namespace egregore.Events
{
    internal sealed class NotifyHubsWhenPageAdded : PageEventHandler
    {
        private readonly IHubContext<NotificationHub> _notify;
        private readonly IHubContext<LiveQueryHub> _queries;

        public NotifyHubsWhenPageAdded(IHubContext<NotificationHub> notify, IHubContext<LiveQueryHub> queries)
        {
            _notify = notify;
            _queries = queries;
        }

        public override Task OnPageAddedAsync(IPageStore store, Page page, CancellationToken cancellationToken = default)
        {
            var pending = AsyncExtensions.TaskPool.Get();
            try
            {
                var notify = _notify.Clients.All.SendAsync(Constants.Notifications.ReceiveMessage, "info", $"Added new page with ID '{page.Uuid}'", cancellationToken);
                if (notify.IsCompleted || notify.IsCanceled || notify.IsFaulted)
                    return AsyncExtensions.NoTask;

                if(notify.Status != TaskStatus.Running)
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