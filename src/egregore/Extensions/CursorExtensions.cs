// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;

namespace egregore.Extensions
{
    internal static class CursorExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts)
        {
            return list
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(x => x.Index / parts)
                .Select(x => x.Select(v => v.Value));
        }
    }
}