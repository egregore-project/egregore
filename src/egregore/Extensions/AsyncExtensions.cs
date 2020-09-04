// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using egregore.Data;
using egregore.Media;
using egregore.Ontology;
using egregore.Pages;
using Microsoft.Extensions.ObjectPool;

namespace egregore.Extensions
{
    /// <summary>
    ///     <remarks>
    ///         `Task.CompletedTask` creates new instances each time the property is accessed, but we want the ability
    ///         to avoid allocations and skip invoking fake or already completed tasks.
    ///     </remarks>
    /// </summary>
    internal static class AsyncExtensions
    {
        public const TaskCreationOptions DoNotRunTask = (TaskCreationOptions) 16384; // InternalTaskOptions.DoNotDispose

        public static readonly Task NoTask = Task.CompletedTask;

        public static readonly ObjectPool<List<Task>> TaskPool =
            new LeakTrackingObjectPool<List<Task>>(new DefaultObjectPool<List<Task>>(new ListPolicy()));

        public static bool IsReal(this Task task)
        {
            return task != default && task != NoTask && !task.CreationOptions.HasFlag(DoNotRunTask);
        }

        public static async Task OnRecordsInitAsync(this IEnumerable<IRecordEventHandler> handlers, IRecordStore store,
            CancellationToken cancellationToken = default)
        {
            var pending = TaskPool.Get();
            try
            {
                foreach (var handler in handlers)
                {
                    var task = handler.OnRecordsInitAsync(store, cancellationToken);

                    if (!task.IsReal())
                        continue; // returns AsyncExtensions.NoTask

                    if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                        continue; // task is already terminal since being instantiated

                    pending.Add(task);
                }

                if (pending.Count > 0)
                    await Task.WhenAll(pending);
            }
            finally
            {
                TaskPool.Return(pending);
            }
        }

        public static async Task OnRecordAddedAsync(this IEnumerable<IRecordEventHandler> handlers, IRecordStore store,
            Record record, CancellationToken cancellationToken = default)
        {
            var pending = TaskPool.Get();
            try
            {
                foreach (var handler in handlers)
                {
                    var task = handler.OnRecordAddedAsync(store, record, cancellationToken);
                    if (!task.IsReal())
                        continue;

                    if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                        continue;

                    pending.Add(task);
                }

                if (pending.Count > 0)
                    await Task.WhenAll(pending);
            }
            finally
            {
                TaskPool.Return(pending);
            }
        }

        public static async Task OnMediaAddedAsync(this IEnumerable<IMediaEventHandler> handlers, IMediaStore store,
            MediaEntry entry, CancellationToken cancellationToken = default)
        {
            var pending = TaskPool.Get();
            try
            {
                foreach (var handler in handlers)
                {
                    var task = handler.OnMediaAddedAsync(store, entry, cancellationToken);
                    if (!task.IsReal())
                        continue;

                    if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                        continue;

                    pending.Add(task);
                }

                if (pending.Count > 0)
                    await Task.WhenAll(pending);
            }
            finally
            {
                TaskPool.Return(pending);
            }
        }

        public static async Task OnSchemaAddedAsync(this IEnumerable<IOntologyEventHandler> handlers, ILogStore store,
            Schema schema, CancellationToken cancellationToken = default)
        {
            var pending = TaskPool.Get();
            try
            {
                foreach (var handler in handlers)
                {
                    var task = handler.OnSchemaAddedAsync(store, schema, cancellationToken);
                    if (!task.IsReal())
                        continue;

                    if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                        continue;

                    pending.Add(task);
                }

                if (pending.Count > 0)
                    await Task.WhenAll(pending);
            }
            finally
            {
                TaskPool.Return(pending);
            }
        }

        public static async Task OnPageAddedAsync(this IEnumerable<IPageEventHandler> handlers, IPageStore store,
            Page page, CancellationToken cancellationToken = default)
        {
            var pending = TaskPool.Get();
            try
            {
                foreach (var handler in handlers)
                {
                    var task = handler.OnPageAddedAsync(store, page, cancellationToken);
                    if (!task.IsReal())
                        continue;

                    if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                        continue;

                    pending.Add(task);
                }

                if (pending.Count > 0)
                    await Task.WhenAll(pending);
            }
            finally
            {
                TaskPool.Return(pending);
            }
        }

        internal sealed class ListPolicy : IPooledObjectPolicy<List<Task>>
        {
            public List<Task> Create()
            {
                return new List<Task>();
            }

            public bool Return(List<Task> list)
            {
                list.Clear();
                return true;
            }
        }
    }
}