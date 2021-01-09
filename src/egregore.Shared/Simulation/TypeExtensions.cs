// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using egregore.Data;

namespace egregore.Simulation
{
    internal static class TypeExtensions
    {
        public static UInt128 Archetype(this IEnumerable<Type> componentTypes, UInt128 seed = default)
        {
            UInt128 archetype = default;
            foreach (var component in componentTypes.OrderBy(x => x.Name))
            {
                var componentId = Hashing.MurmurHash3(component.FullName, seed);
                archetype = componentId ^ archetype;
            }

            return archetype;
        }
    }
}