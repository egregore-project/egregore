// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace egregore.Pooling
{
    public static class ListPool<T>
    {
        private static readonly ObjectPool<List<T>> Pool =
            new LeakTrackingObjectPool<List<T>>(
                new DefaultObjectPool<List<T>>(new ListPolicy<List<T>, T>())
            );

        public static List<T> Get()
        {
            return Pool.Get();
        }

        public static void Return(List<T> obj)
        {
            Pool.Return(obj);
        }

        private class ListPolicy<TCollection, TElement> : IPooledObjectPolicy<TCollection>
            where TCollection : ICollection<TElement>
        {
            public TCollection Create()
            {
                return (TCollection) (ICollection<TElement>) new List<TElement>();
            }

            public bool Return(TCollection collection)
            {
                collection.Clear();
                return collection.Count == 0;
            }
        }
    }
}