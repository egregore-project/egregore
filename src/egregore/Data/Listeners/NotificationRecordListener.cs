// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using egregore.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace egregore.Data.Listeners
{
    internal sealed class NotificationRecordListener : IRecordListener
    {
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationRecordListener(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

        public Task OnRecordsInitAsync(IRecordStore store) => Task.CompletedTask;

        public async Task OnRecordAddedAsync(IRecordStore store, Record record)
        {
            await _hub.Clients.All.SendAsync(Constants.Notifications.ReceiveMessage, "info", $"Added new record with ID '{record.Uuid}'");
        }
    }
}