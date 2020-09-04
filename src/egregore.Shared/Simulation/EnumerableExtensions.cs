// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace egregore.Simulation
{
    internal static class EnumerableExtensions
    {
        public static List<T> TopologicalSort<T>(this IReadOnlyCollection<T> collection,
            Func<T, IEnumerable<T>> getDependentsFunc) where T : IEquatable<T>
        {
            var edges = new List<Tuple<T, T>>();
            foreach (var item in collection)
            {
                var dependents = getDependentsFunc(item);
                foreach (var dependent in dependents) edges.Add(new Tuple<T, T>(item, dependent));
            }

            var sorted = TopologicalSorter<T>.Sort(collection, edges);
            return sorted;
        }
    }
}